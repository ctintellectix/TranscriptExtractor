namespace TranscriptExtractor.Core.Extraction;

public sealed record ExtractionRequest(
    string PromptVersion,
    string SystemPrompt,
    string UserPrompt);
