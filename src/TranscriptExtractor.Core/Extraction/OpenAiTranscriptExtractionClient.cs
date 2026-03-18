using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TranscriptExtractor.Core.Extraction;

public sealed class OpenAiTranscriptExtractionClient(HttpClient httpClient, IOptions<OpenAiExtractionOptions> optionsAccessor) : ITranscriptExtractionClient
{
    private readonly OpenAiExtractionOptions options = optionsAccessor.Value;

    public async Task<TranscriptExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
    {
        var endpoint = new Uri(new Uri(options.BaseUrl.TrimEnd('/') + "/"), "chat/completions");

        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        var body = new
        {
            model = options.Model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        };

        message.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(message, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI extraction failed ({(int)response.StatusCode}): {payload}");
        }

        using var json = JsonDocument.Parse(payload);
        var content = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("OpenAI extraction response did not contain message content.");
        }

        return new TranscriptExtractionResult(content, options.Model);
    }
}
