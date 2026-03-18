namespace TranscriptExtractor.Core.Entities;

public class Transcript
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public string TranscriptText { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public DateTimeOffset? InterviewDateTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Interviewer { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
}
