using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TranscriptExtractor.Core;
using TranscriptExtractor.Core.Entities;

namespace TranscriptExtractor.Tests.Api;

public class ExtractionEndpointsTests : IClassFixture<TranscriptApiFactory>
{
    private readonly TranscriptApiFactory _factory;
    private readonly HttpClient _client;

    public ExtractionEndpointsTests(TranscriptApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTranscriptExtraction_ReturnsSavedExtractionDocument()
    {
        var transcript = new Transcript
        {
            TranscriptText = "Transcript",
            SourceType = "witness_interview"
        };
        var job = new ExtractionJob(transcript.Id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();
        db.Transcripts.Add(transcript);
        db.ExtractionJobs.Add(job);
        db.ExtractionDocuments.Add(new ExtractionDocument(transcript.Id, job.Id, """{ "people": [], "statements": [] }""")
        {
            Model = "gpt-5.4-mini",
            PromptVersion = "lvpd-v1"
        });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/transcripts/{transcript.Id}/extraction");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(transcript.Id, payload.RootElement.GetProperty("transcriptId").GetGuid());
        Assert.Equal("gpt-5.4-mini", payload.RootElement.GetProperty("model").GetString());
        Assert.Equal("lvpd-v1", payload.RootElement.GetProperty("promptVersion").GetString());
    }
}
