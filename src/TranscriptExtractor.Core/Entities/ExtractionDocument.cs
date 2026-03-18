namespace TranscriptExtractor.Core.Entities;

public class ExtractionDocument
{
    public ExtractionDocument(Guid transcriptId, Guid extractionJobId, string json)
    {
        Id = Guid.NewGuid();
        TranscriptId = transcriptId;
        ExtractionJobId = extractionJobId;
        Json = json;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TranscriptId { get; private set; }
    public Guid ExtractionJobId { get; private set; }
    public string Json { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string ReportTemplateVersion { get; set; } = string.Empty;

    private ExtractionDocument()
    {
        Json = string.Empty;
    }
}
