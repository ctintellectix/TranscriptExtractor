using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Core;
using TranscriptExtractor.Core.Entities;

namespace TranscriptExtractor.Tests.Core;

public class TranscriptExtractorDbContextTests
{
    [Fact]
    public async Task SaveChanges_PersistsTranscriptJobAndDocument()
    {
        var options = new DbContextOptionsBuilder<TranscriptExtractorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("n"))
            .Options;

        await using var db = new TranscriptExtractorDbContext(options);

        var transcript = new Transcript
        {
            TranscriptText = "Witness described an argument outside the residence.",
            SourceType = "witness_interview"
        };
        var job = new ExtractionJob(transcript.Id);
        var document = new ExtractionDocument(transcript.Id, job.Id, """{ "people": [], "statements": [] }""");

        db.Transcripts.Add(transcript);
        db.ExtractionJobs.Add(job);
        db.ExtractionDocuments.Add(document);

        await db.SaveChangesAsync();

        Assert.Equal(1, await db.Transcripts.CountAsync());
        Assert.Equal(1, await db.ExtractionJobs.CountAsync());
        Assert.Equal(1, await db.ExtractionDocuments.CountAsync());
    }
}
