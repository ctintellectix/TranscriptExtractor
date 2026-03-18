using System.Net;
using Microsoft.Extensions.DependencyInjection;
using TranscriptExtractor.Core;
using TranscriptExtractor.Core.Entities;
using TranscriptExtractor.Core.Reports;

namespace TranscriptExtractor.Tests.Api;

public class PdfEndpointTests : IClassFixture<TranscriptApiFactory>
{
    private readonly TranscriptApiFactory _factory;
    private readonly HttpClient _client;

    public PdfEndpointTests(TranscriptApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTranscriptPdf_ReturnsConflictWhenExtractionIsMissing()
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
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/reports/transcripts/{transcript.Id}/pdf");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetTranscriptPdf_ReturnsHtmlBackedPdfPayloadWhenExtractionExists()
    {
        var transcript = new Transcript
        {
            TranscriptText = "Transcript",
            SourceType = "witness_interview"
        };
        var job = new ExtractionJob(transcript.Id);
        job.MarkCompleted("gpt-5.4-mini", "lvpd-v1");

        const string json = """
        {
          "transcript_metadata": {
            "transcript_id": "t1",
            "datetime": null,
            "location": null,
            "interviewer": null,
            "source_type": "witness_interview"
          },
          "people": [],
          "locations": [],
          "objects": [],
          "statements": [],
          "described_events": [],
          "relationship_claims": [],
          "allegations": [],
          "emotional_behavioral_cues": [],
          "contradictions": []
        }
        """;

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();
        db.Transcripts.Add(transcript);
        db.ExtractionJobs.Add(job);
        db.ExtractionDocuments.Add(new ExtractionDocument(transcript.Id, job.Id, json)
        {
            Model = "gpt-5.4-mini",
            PromptVersion = "lvpd-v1",
            ReportTemplateVersion = ReportTemplateVersion.Current
        });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/reports/transcripts/{transcript.Id}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
