namespace TranscriptExtractor.Api.Contracts;

public sealed class TranscriptStatusResponse
{
    public Guid TranscriptId { get; set; }
    public string TranscriptText { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Interviewer { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? InterviewDateTime { get; set; }
    public string JobStatus { get; set; } = string.Empty;
}
