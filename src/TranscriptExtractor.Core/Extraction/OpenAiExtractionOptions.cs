namespace TranscriptExtractor.Core.Extraction;

public sealed class OpenAiExtractionOptions
{
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string Model { get; set; } = "gpt-5.4-mini";
    public string ApiKey { get; set; } = string.Empty;
}
