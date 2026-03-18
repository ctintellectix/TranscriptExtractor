using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Core;
using TranscriptExtractor.Core.Entities;
using TranscriptExtractor.Core.Extraction;
using TranscriptExtractor.Core.Prompts;

namespace TranscriptExtractor.Tests.Worker;

public class TranscriptExtractionOrchestratorTests
{
    [Fact]
    public async Task ProcessOneAsync_ProcessesOldestQueuedJobAndPersistsExtractionDocument()
    {
        var options = new DbContextOptionsBuilder<TranscriptExtractorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("n"))
            .Options;

        await using var db = new TranscriptExtractorDbContext(options);
        var olderTranscript = new Transcript { TranscriptText = "Older transcript.", SourceType = "witness_interview" };
        var newerTranscript = new Transcript { TranscriptText = "Newer transcript.", SourceType = "suspect_interview" };
        var olderJob = new ExtractionJob(olderTranscript.Id);
        var newerJob = new ExtractionJob(newerTranscript.Id);

        db.Transcripts.AddRange(olderTranscript, newerTranscript);
        db.ExtractionJobs.AddRange(olderJob, newerJob);
        await db.SaveChangesAsync();

        var orchestrator = new TranscriptExtractionOrchestrator(
            db,
            new FakePromptAssetLoader(new PromptAssets("lvpd-v1", "System", "Prompt <<<TRANSCRIPT_TEXT>>> <<<JSON OUTPUT SCHEMA>>>", "{ }")),
            new FakeTranscriptExtractionClient("""{ "people": [], "statements": [] }""", "gpt-5.4-mini"),
            promptDirectory: "unused");

        var processed = await orchestrator.ProcessOneAsync(CancellationToken.None);

        Assert.True(processed);

        var refreshedOlderJob = await db.ExtractionJobs.SingleAsync(x => x.Id == olderJob.Id);
        var refreshedNewerJob = await db.ExtractionJobs.SingleAsync(x => x.Id == newerJob.Id);
        var document = await db.ExtractionDocuments.SingleAsync();

        Assert.Equal(ExtractionJobStatus.Completed, refreshedOlderJob.Status);
        Assert.Equal(ExtractionJobStatus.Queued, refreshedNewerJob.Status);
        Assert.Equal(olderTranscript.Id, document.TranscriptId);
        Assert.Equal("""{ "people": [], "statements": [] }""", document.Json);
        Assert.Equal("gpt-5.4-mini", document.Model);
        Assert.Equal("lvpd-v1", document.PromptVersion);
    }

    [Fact]
    public async Task ProcessOneAsync_MarksJobFailedWhenClientThrows()
    {
        var options = new DbContextOptionsBuilder<TranscriptExtractorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("n"))
            .Options;

        await using var db = new TranscriptExtractorDbContext(options);
        var transcript = new Transcript { TranscriptText = "Transcript text.", SourceType = "witness_interview" };
        var job = new ExtractionJob(transcript.Id);

        db.Transcripts.Add(transcript);
        db.ExtractionJobs.Add(job);
        await db.SaveChangesAsync();

        var orchestrator = new TranscriptExtractionOrchestrator(
            db,
            new FakePromptAssetLoader(new PromptAssets("lvpd-v1", "System", "Prompt <<<TRANSCRIPT_TEXT>>>", "{ }")),
            new ThrowingTranscriptExtractionClient("boom"),
            promptDirectory: "unused");

        var processed = await orchestrator.ProcessOneAsync(CancellationToken.None);

        Assert.True(processed);

        var refreshedJob = await db.ExtractionJobs.SingleAsync();
        Assert.Equal(ExtractionJobStatus.Failed, refreshedJob.Status);
        Assert.Equal("boom", refreshedJob.Error);
        Assert.Equal(1, refreshedJob.RetryCount);
        Assert.Equal(0, await db.ExtractionDocuments.CountAsync());
    }

    private sealed class FakePromptAssetLoader(PromptAssets assets) : IPromptAssetLoader
    {
        public PromptAssets Load(string promptDirectory) => assets;
    }

    private sealed class FakeTranscriptExtractionClient(string json, string model) : ITranscriptExtractionClient
    {
        public Task<TranscriptExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TranscriptExtractionResult(json, model));
        }
    }

    private sealed class ThrowingTranscriptExtractionClient(string message) : ITranscriptExtractionClient
    {
        public Task<TranscriptExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(message);
        }
    }
}
