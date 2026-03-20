namespace TranscriptExtractor.Api.Contracts;

public sealed class DashboardSummaryResponse
{
    public int QueuedCount { get; set; }
    public int ProcessingCount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTimeOffset? LatestTranscriptReceivedAt { get; set; }
    public DateTimeOffset? LatestJobCreatedAt { get; set; }
    public DateTimeOffset? LatestJobUpdatedAt { get; set; }
    public DateTimeOffset? LatestCompletedAt { get; set; }
    public DateTimeOffset? LatestFailedAt { get; set; }
}
