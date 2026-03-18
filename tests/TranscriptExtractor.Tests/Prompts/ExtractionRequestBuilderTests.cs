using TranscriptExtractor.Core.Entities;
using TranscriptExtractor.Core.Extraction;
using TranscriptExtractor.Core.Prompts;

namespace TranscriptExtractor.Tests.Prompts;

public class ExtractionRequestBuilderTests
{
    [Fact]
    public void Build_InsertsTranscriptTextAndSchemaIntoUserPrompt()
    {
        var transcript = new Transcript
        {
            TranscriptText = "Emily Carter said she saw an argument outside the residence.",
            SourceType = "witness_interview"
        };
        var assets = new PromptAssets(
            Version: "lvpd-v1",
            SystemPrompt: "System prompt",
            UserPromptTemplate: """
                Transcript:
                <<<TRANSCRIPT_TEXT>>>

                Schema:
                <<<JSON OUTPUT SCHEMA>>>
                """,
            SchemaText: "{ \"people\": [] }");

        var request = ExtractionRequestBuilder.Build(transcript, assets);

        Assert.Equal("lvpd-v1", request.PromptVersion);
        Assert.Equal("System prompt", request.SystemPrompt);
        Assert.Contains(transcript.TranscriptText, request.UserPrompt, StringComparison.Ordinal);
        Assert.Contains(assets.SchemaText, request.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("<<<TRANSCRIPT_TEXT>>>", request.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("<<<JSON OUTPUT SCHEMA>>>", request.UserPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_PreservesFixedPromptVersionFromAssets()
    {
        var transcript = new Transcript { TranscriptText = "Short transcript." };
        var assets = new PromptAssets(
            Version: "lvpd-fixed-v1",
            SystemPrompt: "System",
            UserPromptTemplate: "<<<TRANSCRIPT_TEXT>>>",
            SchemaText: "{}");

        var request = ExtractionRequestBuilder.Build(transcript, assets);

        Assert.Equal("lvpd-fixed-v1", request.PromptVersion);
    }
}
