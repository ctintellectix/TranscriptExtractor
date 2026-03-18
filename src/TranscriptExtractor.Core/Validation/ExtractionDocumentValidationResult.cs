namespace TranscriptExtractor.Core.Validation;

public sealed class ExtractionDocumentValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
}
