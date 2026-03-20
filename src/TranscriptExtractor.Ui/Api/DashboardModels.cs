namespace TranscriptExtractor.Ui.Api;

public sealed record DashboardSummaryModel
{
    public int QueuedCount { get; init; }
    public int ProcessingCount { get; init; }
    public int CompletedCount { get; init; }
    public int FailedCount { get; init; }
    public DateTimeOffset? LatestTranscriptReceivedAt { get; init; }
    public DateTimeOffset? LatestJobCreatedAt { get; init; }
    public DateTimeOffset? LatestJobUpdatedAt { get; init; }
    public DateTimeOffset? LatestCompletedAt { get; init; }
    public DateTimeOffset? LatestFailedAt { get; init; }
}

public sealed record RecentTranscriptModel
{
    public Guid TranscriptId { get; init; }
    public Guid JobId { get; init; }
    public string? CaseNumber { get; init; }
    public string? Interviewer { get; init; }
    public string? SourceType { get; init; }
    public string? JobStatus { get; init; }
    public string? FailureMessage { get; init; }
    public DateTimeOffset? TranscriptReceivedAt { get; init; }
    public DateTimeOffset? JobCreatedAt { get; init; }
    public DateTimeOffset? JobUpdatedAt { get; init; }
    public DateTimeOffset? JobStartedAt { get; init; }
    public DateTimeOffset? JobCompletedAt { get; init; }
    public DateTimeOffset? ActivityAt { get; init; }
}

public sealed record WorkerHealthModel
{
    public string? WorkerName { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? LastPollAt { get; init; }
    public DateTimeOffset? LastSuccessfulJobAt { get; init; }
    public DateTimeOffset? LastErrorAt { get; init; }
    public string? LastError { get; init; }
}
