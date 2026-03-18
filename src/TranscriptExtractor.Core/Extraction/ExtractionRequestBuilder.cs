using TranscriptExtractor.Core.Entities;
using TranscriptExtractor.Core.Prompts;

namespace TranscriptExtractor.Core.Extraction;

public static class ExtractionRequestBuilder
{
    public static ExtractionRequest Build(Transcript transcript, PromptAssets assets)
    {
        var userPrompt = assets.UserPromptTemplate
            .Replace("<<<TRANSCRIPT_TEXT>>>", transcript.TranscriptText, StringComparison.Ordinal)
            .Replace("<<<JSON OUTPUT SCHEMA>>>", assets.SchemaText, StringComparison.Ordinal);

        return new ExtractionRequest(
            PromptVersion: assets.Version,
            SystemPrompt: assets.SystemPrompt,
            UserPrompt: userPrompt);
    }
}
