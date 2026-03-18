using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Api.Contracts;
using TranscriptExtractor.Core;
using TranscriptExtractor.Core.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TranscriptExtractorDbContext>(options =>
    options.UseInMemoryDatabase("TranscriptExtractor"));

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

app.MapGet("/", () => "Hello World!");

app.Run();

public partial class Program;
