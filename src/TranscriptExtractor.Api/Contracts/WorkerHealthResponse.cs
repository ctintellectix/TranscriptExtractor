namespace TranscriptExtractor.Api.Contracts;

public sealed class WorkerHealthResponse
{
    public string WorkerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? LastPollAt { get; set; }
    public DateTimeOffset? LastSuccessfulJobAt { get; set; }
    public DateTimeOffset? LastErrorAt { get; set; }
    public string? LastError { get; set; }
}
