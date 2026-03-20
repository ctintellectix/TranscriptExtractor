namespace TranscriptExtractor.Ui.Api;

public sealed class TranscriptExtractorApiClient(HttpClient httpClient)
{
    public HttpClient HttpClient { get; } = httpClient;
}
