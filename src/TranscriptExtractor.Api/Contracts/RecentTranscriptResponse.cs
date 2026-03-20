namespace TranscriptExtractor.Api.Contracts;

public sealed class RecentTranscriptResponse
{
    public Guid TranscriptId { get; set; }
    public Guid JobId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Interviewer { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string JobStatus { get; set; } = string.Empty;
    public string? FailureMessage { get; set; }
    public DateTimeOffset TranscriptReceivedAt { get; set; }
    public DateTimeOffset JobCreatedAt { get; set; }
    public DateTimeOffset JobUpdatedAt { get; set; }
    public DateTimeOffset? JobStartedAt { get; set; }
    public DateTimeOffset? JobCompletedAt { get; set; }
    public DateTimeOffset ActivityAt { get; set; }
}
