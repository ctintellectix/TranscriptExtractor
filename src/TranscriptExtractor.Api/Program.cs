using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Api.Contracts;
using TranscriptExtractor.Core;
using TranscriptExtractor.Core.Entities;
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
builder.Services.AddSingleton<ITranscriptPdfRenderer, QuestTranscriptPdfRenderer>();

var app = builder.Build();

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

public partial class Program;
