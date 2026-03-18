using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TranscriptExtractor.Tests.Api;

public class TranscriptEndpointsTests : IClassFixture<TranscriptApiFactory>
{
    private readonly HttpClient _client;

    public TranscriptEndpointsTests(TranscriptApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostTranscripts_CreatesTranscriptAndQueuedJob()
    {
        var response = await _client.PostAsJsonAsync("/transcripts", new
        {
            transcriptText = "Emily Carter said she saw an argument outside the residence.",
            sourceType = "witness_interview",
            caseNumber = "CASE-1001",
            interviewer = "Detective Stone",
            location = "Brookfield",
            interviewDateTime = "2025-07-14T21:15:00Z"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(payload.RootElement.TryGetProperty("transcriptId", out var transcriptId));
        Assert.True(payload.RootElement.TryGetProperty("jobStatus", out var jobStatus));
        Assert.NotEqual(Guid.Empty, transcriptId.GetGuid());
        Assert.Equal("Queued", jobStatus.GetString());
    }

    [Fact]
    public async Task GetTranscript_ReturnsTranscriptMetadataAndJobStatus()
    {
        var createResponse = await _client.PostAsJsonAsync("/transcripts", new
        {
            transcriptText = "Emily Carter said she saw an argument outside the residence.",
            sourceType = "witness_interview"
        });

        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var transcriptId = created.RootElement.GetProperty("transcriptId").GetGuid();

        var response = await _client.GetAsync($"/transcripts/{transcriptId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(transcriptId, payload.RootElement.GetProperty("transcriptId").GetGuid());
        Assert.Equal("witness_interview", payload.RootElement.GetProperty("sourceType").GetString());
        Assert.Equal("Queued", payload.RootElement.GetProperty("jobStatus").GetString());
    }
}
