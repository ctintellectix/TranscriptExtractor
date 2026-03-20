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

    [Fact]
    public async Task SaveChanges_PersistsWorkerHeartbeat()
    {
        var options = new DbContextOptionsBuilder<TranscriptExtractorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("n"))
            .Options;

        await using var db = new TranscriptExtractorDbContext(options);

        var entityType = db.Model.FindEntityType(typeof(WorkerHeartbeat));

        Assert.NotNull(entityType);
        Assert.Equal(128, entityType!.FindProperty(nameof(WorkerHeartbeat.WorkerName))!.GetMaxLength());
        Assert.Equal("text", entityType.FindProperty(nameof(WorkerHeartbeat.LastError))!
            .FindAnnotation("Relational:ColumnType")!.Value);

        var workerNameIndex = entityType.GetIndexes()
            .Single(index => index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(WorkerHeartbeat.WorkerName) }));

        Assert.True(workerNameIndex.IsUnique);

        var heartbeat = new WorkerHeartbeat("worker-a")
        {
            LastPollAt = new DateTimeOffset(2026, 3, 19, 13, 0, 0, TimeSpan.Zero),
            LastSuccessfulJobAt = new DateTimeOffset(2026, 3, 19, 12, 45, 0, TimeSpan.Zero),
            LastErrorAt = new DateTimeOffset(2026, 3, 19, 12, 50, 0, TimeSpan.Zero),
            LastError = "temporary failure"
        };

        db.WorkerHeartbeats.Add(heartbeat);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var persisted = await db.WorkerHeartbeats.SingleAsync();

        Assert.Equal("worker-a", persisted.WorkerName);
        Assert.Equal(new DateTimeOffset(2026, 3, 19, 13, 0, 0, TimeSpan.Zero), persisted.LastPollAt);
        Assert.Equal(new DateTimeOffset(2026, 3, 19, 12, 45, 0, TimeSpan.Zero), persisted.LastSuccessfulJobAt);
        Assert.Equal(new DateTimeOffset(2026, 3, 19, 12, 50, 0, TimeSpan.Zero), persisted.LastErrorAt);
        Assert.Equal("temporary failure", persisted.LastError);
    }

    [Fact]
    public void WorkerHeartbeat_RejectsBlankWorkerName()
    {
        Assert.Throws<ArgumentException>(() => new WorkerHeartbeat(" "));
    }
}
