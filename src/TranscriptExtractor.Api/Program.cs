using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Api.Startup;
using TranscriptExtractor.Api.Contracts;
using TranscriptExtractor.Core;
using TranscriptExtractor.Core.Entities;
using TranscriptExtractor.Core.Maps;
using TranscriptExtractor.Core.Persistence;
using TranscriptExtractor.Core.Reports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TranscriptExtractorDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.UseInMemoryDatabase("TranscriptExtractorTests");
        return;
    }

    TranscriptExtractorDbContextConfigurator.ConfigurePostgres(
        options,
        builder.Configuration.GetConnectionString("TranscriptExtractor")
            ?? throw new InvalidOperationException("Connection string 'TranscriptExtractor' is required."));
});
builder.Services.AddScoped<IDatabaseMigrationRunner, DatabaseMigrationRunner>();
builder.Services.AddHttpClient<IAddressGeocoder, OpenStreetMapAddressGeocoder>();
builder.Services.AddHttpClient<IStaticMapRenderer, OpenStreetMapStaticMapRenderer>();
builder.Services.AddScoped<ITranscriptPdfRenderer, QuestTranscriptPdfRenderer>();

var app = builder.Build();

await DatabaseMigration.ApplyAsync(
    app.Environment,
    app.Services,
    CancellationToken.None);

app.MapPost("/transcripts", async (CreateTranscriptRequest request, TranscriptExtractorDbContext db) =>
{
    var transcript = new Transcript
    {
        TranscriptText = request.TranscriptText,
        CaseNumber = request.CaseNumber,
        SourceType = request.SourceType,
        Interviewer = request.Interviewer,
        Location = request.Location,
        InterviewDateTime = request.InterviewDateTime
    };

    var job = new ExtractionJob(transcript.Id);

    db.Transcripts.Add(transcript);
    db.ExtractionJobs.Add(job);
    await db.SaveChangesAsync();

    return Results.Created($"/transcripts/{transcript.Id}", new
    {
        transcriptId = transcript.Id,
        jobStatus = job.Status.ToString()
    });
});

app.MapGet("/dashboard/summary", async (TranscriptExtractorDbContext db) =>
{
    var queuedCount = await db.ExtractionJobs.CountAsync(x => x.Status == ExtractionJobStatus.Queued);
    var processingCount = await db.ExtractionJobs.CountAsync(x => x.Status == ExtractionJobStatus.Processing);
    var completedCount = await db.ExtractionJobs.CountAsync(x => x.Status == ExtractionJobStatus.Completed);
    var failedCount = await db.ExtractionJobs.CountAsync(x => x.Status == ExtractionJobStatus.Failed);

    var latestTranscriptReceivedAt = await db.Transcripts
        .Select(x => (DateTimeOffset?)x.ReceivedAt)
        .MaxAsync();

    var latestJobCreatedAt = await db.ExtractionJobs
        .Select(x => (DateTimeOffset?)x.CreatedAt)
        .MaxAsync();

    var latestJobUpdatedAt = await db.ExtractionJobs
        .Select(x => (DateTimeOffset?)x.UpdatedAt)
        .MaxAsync();

    var latestCompletedAt = await db.ExtractionJobs
        .Where(x => x.Status == ExtractionJobStatus.Completed)
        .Select(x => x.CompletedAt)
        .MaxAsync();

    var latestFailedAt = await db.ExtractionJobs
        .Where(x => x.Status == ExtractionJobStatus.Failed)
        .Select(x => x.CompletedAt)
        .MaxAsync();

    return Results.Ok(new DashboardSummaryResponse
    {
        QueuedCount = queuedCount,
        ProcessingCount = processingCount,
        CompletedCount = completedCount,
        FailedCount = failedCount,
        LatestTranscriptReceivedAt = latestTranscriptReceivedAt,
        LatestJobCreatedAt = latestJobCreatedAt,
        LatestJobUpdatedAt = latestJobUpdatedAt,
        LatestCompletedAt = latestCompletedAt,
        LatestFailedAt = latestFailedAt
    });
});

app.MapGet("/dashboard/recent", async (TranscriptExtractorDbContext db) =>
{
    var recent = await (
        from transcript in db.Transcripts
        let job = db.ExtractionJobs
            .Where(x => x.TranscriptId == transcript.Id)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault()
        where job != null
        orderby job.UpdatedAt descending, job.CreatedAt descending, transcript.Id descending
        select new RecentTranscriptResponse
        {
            TranscriptId = transcript.Id,
            JobId = job.Id,
            CaseNumber = transcript.CaseNumber,
            Interviewer = transcript.Interviewer,
            SourceType = transcript.SourceType,
            JobStatus = job.Status.ToString(),
            FailureMessage = job.Error,
            TranscriptReceivedAt = transcript.ReceivedAt,
            JobCreatedAt = job.CreatedAt,
            JobUpdatedAt = job.UpdatedAt,
            JobStartedAt = job.StartedAt,
            JobCompletedAt = job.CompletedAt,
            ActivityAt = job.UpdatedAt
        })
        .Take(10)
        .ToListAsync();

    return Results.Ok(recent);
});

app.MapGet("/worker/health", async (TranscriptExtractorDbContext db) =>
{
    var heartbeat = await db.WorkerHeartbeats
        .OrderByDescending(x => x.LastPollAt)
        .FirstOrDefaultAsync();

    if (heartbeat is null)
    {
        return Results.Ok(new WorkerHealthResponse
        {
            Status = "offline"
        });
    }

    var status = GetWorkerHealthStatus(heartbeat, DateTimeOffset.UtcNow);

    return Results.Ok(new WorkerHealthResponse
    {
        WorkerName = heartbeat.WorkerName,
        Status = status,
        LastPollAt = heartbeat.LastPollAt,
        LastSuccessfulJobAt = heartbeat.LastSuccessfulJobAt,
        LastErrorAt = heartbeat.LastErrorAt,
        LastError = heartbeat.LastError
    });
});

app.MapGet("/transcripts/{id:guid}", async (Guid id, TranscriptExtractorDbContext db) =>
{
    var transcript = await db.Transcripts.FindAsync(id);
    if (transcript is null)
    {
        return Results.NotFound();
    }

    var job = await db.ExtractionJobs
        .OrderByDescending(x => x.CreatedAt)
        .FirstOrDefaultAsync(x => x.TranscriptId == id);

    return Results.Ok(new TranscriptStatusResponse
    {
        TranscriptId = transcript.Id,
        TranscriptText = transcript.TranscriptText,
        CaseNumber = transcript.CaseNumber,
        SourceType = transcript.SourceType,
        Interviewer = transcript.Interviewer,
        Location = transcript.Location,
        ReceivedAt = transcript.ReceivedAt,
        InterviewDateTime = transcript.InterviewDateTime,
        JobStatus = job?.Status.ToString() ?? ExtractionJobStatus.Queued.ToString()
    });
});

app.MapGet("/transcripts/{id:guid}/extraction", async (Guid id, TranscriptExtractorDbContext db) =>
{
    var document = await db.ExtractionDocuments
        .OrderByDescending(x => x.CreatedAt)
        .FirstOrDefaultAsync(x => x.TranscriptId == id);

    if (document is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        transcriptId = document.TranscriptId,
        extractionDocumentId = document.Id,
        model = document.Model,
        promptVersion = document.PromptVersion,
        json = document.Json
    });
});

app.MapGet("/reports/transcripts/{id:guid}/pdf", async (
    Guid id,
    TranscriptExtractorDbContext db,
    ITranscriptPdfRenderer pdfRenderer) =>
{
    var transcript = await db.Transcripts.FindAsync(id);
    if (transcript is null)
    {
        return Results.NotFound();
    }

    var document = await db.ExtractionDocuments
        .OrderByDescending(x => x.CreatedAt)
        .FirstOrDefaultAsync(x => x.TranscriptId == id);

    if (document is null)
    {
        return Results.Conflict(new { message = "Extraction not ready." });
    }

    var report = TranscriptReportComposer.Compose(document.Json);
    var templateVersion = string.IsNullOrWhiteSpace(document.ReportTemplateVersion)
        ? ReportTemplateVersion.Current
        : document.ReportTemplateVersion;
    var html = TranscriptReportHtmlRenderer.Render(report, templateVersion);
    var pdf = pdfRenderer.RenderPdf(html, templateVersion);

    return Results.File(pdf, "application/pdf", $"transcript-{id}.pdf");
});

app.MapGet("/", () => "Hello World!");

app.Run();

public partial class Program
{
    private const int WorkerStaleThresholdMinutes = 15;

    internal static string GetWorkerHealthStatus(WorkerHeartbeat heartbeat, DateTimeOffset now)
    {
        var staleThreshold = TimeSpan.FromMinutes(WorkerStaleThresholdMinutes);
        if (now - heartbeat.LastPollAt > staleThreshold)
        {
            return "stale";
        }

        if (heartbeat.LastErrorAt is not null &&
            (heartbeat.LastSuccessfulJobAt is null || heartbeat.LastErrorAt >= heartbeat.LastSuccessfulJobAt))
        {
            return "idle";
        }

        return heartbeat.LastSuccessfulJobAt is not null ? "healthy" : "idle";
    }
}
