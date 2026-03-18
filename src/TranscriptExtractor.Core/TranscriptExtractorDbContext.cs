using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Core.Entities;

namespace TranscriptExtractor.Core;

public class TranscriptExtractorDbContext(DbContextOptions<TranscriptExtractorDbContext> options) : DbContext(options)
{
    public DbSet<Transcript> Transcripts => Set<Transcript>();
    public DbSet<ExtractionJob> ExtractionJobs => Set<ExtractionJob>();
    public DbSet<ExtractionDocument> ExtractionDocuments => Set<ExtractionDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transcript>()
            .Property(x => x.SourceType)
            .HasMaxLength(128);

        modelBuilder.Entity<ExtractionJob>()
            .HasIndex(x => new { x.Status, x.CreatedAt });

        modelBuilder.Entity<ExtractionDocument>()
            .HasIndex(x => x.TranscriptId)
            .IsUnique();
    }
}
