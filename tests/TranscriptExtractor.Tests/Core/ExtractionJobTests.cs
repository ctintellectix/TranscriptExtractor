using TranscriptExtractor.Core.Entities;

namespace TranscriptExtractor.Tests.Core;

public class ExtractionJobTests
{
    [Fact]
    public void NewExtractionJob_StartsQueuedAndAttachedToTranscript()
    {
        var transcriptId = Guid.NewGuid();

        var job = new ExtractionJob(transcriptId);

        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal(transcriptId, job.TranscriptId);
        Assert.Equal(ExtractionJobStatus.Queued, job.Status);
        Assert.Equal(0, job.RetryCount);
        Assert.Null(job.Error);
    }

    [Fact]
    public void MarkProcessing_SetsStatusAndStartedAt()
    {
        var job = new ExtractionJob(Guid.NewGuid());

        job.MarkProcessing();

        Assert.Equal(ExtractionJobStatus.Processing, job.Status);
        Assert.NotNull(job.StartedAt);
        Assert.True(job.UpdatedAt >= job.CreatedAt);
    }

    [Fact]
    public void MarkCompleted_CapturesModelAndPromptVersion()
    {
        var job = new ExtractionJob(Guid.NewGuid());

        job.MarkProcessing();
        job.MarkCompleted("gpt-5.4-mini", "lvpd-v1");

        Assert.Equal(ExtractionJobStatus.Completed, job.Status);
        Assert.Equal("gpt-5.4-mini", job.Model);
        Assert.Equal("lvpd-v1", job.PromptVersion);
        Assert.NotNull(job.CompletedAt);
        Assert.Null(job.Error);
    }

    [Fact]
    public void MarkFailed_IncrementsRetryCountAndStoresError()
    {
        var job = new ExtractionJob(Guid.NewGuid());

        job.MarkFailed("invalid json");

        Assert.Equal(ExtractionJobStatus.Failed, job.Status);
        Assert.Equal(1, job.RetryCount);
        Assert.Equal("invalid json", job.Error);
        Assert.NotNull(job.CompletedAt);
    }
}
