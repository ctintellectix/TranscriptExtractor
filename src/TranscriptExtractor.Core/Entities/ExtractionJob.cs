namespace TranscriptExtractor.Core.Entities;

public class ExtractionJob
{
    public ExtractionJob(Guid transcriptId)
    {
        Id = Guid.NewGuid();
        TranscriptId = transcriptId;
        Status = ExtractionJobStatus.Queued;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid TranscriptId { get; private set; }
    public ExtractionJobStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }
    public string Model { get; private set; } = string.Empty;
    public string PromptVersion { get; private set; } = string.Empty;

    private ExtractionJob()
    {
    }

    public void MarkProcessing()
    {
        Status = ExtractionJobStatus.Processing;
        StartedAt ??= DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        Error = null;
    }

    public void MarkCompleted(string model, string promptVersion)
    {
        Status = ExtractionJobStatus.Completed;
        Model = model;
        PromptVersion = promptVersion;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CompletedAt.Value;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        Status = ExtractionJobStatus.Failed;
        Error = error;
        RetryCount++;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CompletedAt.Value;
    }
}
