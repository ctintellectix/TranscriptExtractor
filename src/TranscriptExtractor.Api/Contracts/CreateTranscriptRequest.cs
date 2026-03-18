namespace TranscriptExtractor.Api.Contracts;

public sealed class CreateTranscriptRequest
{
    public string TranscriptText { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Interviewer { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTimeOffset? InterviewDateTime { get; set; }
}
