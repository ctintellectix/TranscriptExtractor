using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TranscriptExtractor.Core.Extraction;

namespace TranscriptExtractor.Tests.Extraction;

public class OpenAiTranscriptExtractionClientTests
{
    [Fact]
    public async Task ExtractAsync_ReturnsJsonAndConfiguredModelFromChatCompletionsResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedPayload = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            capturedPayload = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();

            var responseJson = """
            {
              "choices": [
                {
                  "message": {
                    "content": "{ \"people\": [], \"statements\": [] }"
                  }
                }
              ]
            }
            """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        var client = new OpenAiTranscriptExtractionClient(
            httpClient,
            Options.Create(new OpenAiExtractionOptions
            {
                ApiKey = "test-key",
                BaseUrl = "https://api.openai.com/v1/",
                Model = "gpt-5.4-mini"
            }));

        var result = await client.ExtractAsync(
            new ExtractionRequest("lvpd-v1", "system prompt", "user prompt"),
            CancellationToken.None);

        Assert.Equal("{ \"people\": [], \"statements\": [] }", result.Json);
        Assert.Equal("gpt-5.4-mini", result.Model);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("https://api.openai.com/v1/chat/completions", capturedRequest.RequestUri?.ToString());
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("test-key", capturedRequest.Headers.Authorization?.Parameter);

        using var payload = JsonDocument.Parse(capturedPayload!);
        Assert.Equal("gpt-5.4-mini", payload.RootElement.GetProperty("model").GetString());
        Assert.Equal("system", payload.RootElement.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal("user", payload.RootElement.GetProperty("messages")[1].GetProperty("role").GetString());
    }

    [Fact]
    public async Task ExtractAsync_ThrowsWhenOpenAiReturnsNonSuccess()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"bad request\"}", Encoding.UTF8, "application/json")
            });

        using var httpClient = new HttpClient(handler);
        var client = new OpenAiTranscriptExtractionClient(
            httpClient,
            Options.Create(new OpenAiExtractionOptions
            {
                ApiKey = "test-key",
                BaseUrl = "https://api.openai.com/v1/",
                Model = "gpt-5.4-mini"
            }));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ExtractAsync(new ExtractionRequest("lvpd-v1", "system prompt", "user prompt"), CancellationToken.None));

        Assert.Contains("400", error.Message, StringComparison.Ordinal);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
