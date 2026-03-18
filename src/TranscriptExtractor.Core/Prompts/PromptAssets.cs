namespace TranscriptExtractor.Core.Prompts;

public sealed record PromptAssets(
    string Version,
    string SystemPrompt,
    string UserPromptTemplate,
    string SchemaText);
