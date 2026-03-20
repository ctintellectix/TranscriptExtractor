using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Core.Entities;

namespace TranscriptExtractor.Core;

public class TranscriptExtractorDbContext(DbContextOptions<TranscriptExtractorDbContext> options) : DbContext(options)
{
    public DbSet<Transcript> Transcripts => Set<Transcript>();
    public DbSet<ExtractionJob> ExtractionJobs => Set<ExtractionJob>();
    public DbSet<ExtractionDocument> ExtractionDocuments => Set<ExtractionDocument>();
    public DbSet<WorkerHeartbeat> WorkerHeartbeats => Set<WorkerHeartbeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transcript>()
            .Property(x => x.TranscriptText)
            .HasColumnType("text");

        modelBuilder.Entity<Transcript>()
            .Property(x => x.SourceType)
            .HasMaxLength(128);

        modelBuilder.Entity<Transcript>()
            .Property(x => x.CaseNumber)
            .HasMaxLength(128);

        modelBuilder.Entity<Transcript>()
            .Property(x => x.Interviewer)
            .HasMaxLength(256);

        modelBuilder.Entity<Transcript>()
            .Property(x => x.Location)
            .HasMaxLength(256);

        modelBuilder.Entity<Transcript>()
            .Property(x => x.SourceFileName)
            .HasMaxLength(512);

        modelBuilder.Entity<ExtractionJob>()
            .Property(x => x.Model)
            .HasMaxLength(128);

        modelBuilder.Entity<ExtractionJob>()
            .Property(x => x.PromptVersion)
            .HasMaxLength(128);

        modelBuilder.Entity<ExtractionJob>()
            .Property(x => x.Error)
            .HasColumnType("text");

        modelBuilder.Entity<ExtractionJob>()
            .HasIndex(x => new { x.Status, x.CreatedAt });

        modelBuilder.Entity<ExtractionDocument>()
            .Property(x => x.Json)
            .HasColumnType("jsonb");

        modelBuilder.Entity<ExtractionDocument>()
            .Property(x => x.Model)
            .HasMaxLength(128);

        modelBuilder.Entity<ExtractionDocument>()
            .Property(x => x.PromptVersion)
            .HasMaxLength(128);

        modelBuilder.Entity<ExtractionDocument>()
            .Property(x => x.ReportTemplateVersion)
            .HasMaxLength(128);

        modelBuilder.Entity<ExtractionDocument>()
            .HasIndex(x => x.TranscriptId)
            .IsUnique();

        modelBuilder.Entity<WorkerHeartbeat>()
            .Property(x => x.WorkerName)
            .HasMaxLength(128);

        modelBuilder.Entity<WorkerHeartbeat>()
            .Property(x => x.LastError)
            .HasColumnType("text");

        modelBuilder.Entity<WorkerHeartbeat>()
            .HasIndex(x => x.WorkerName)
            .IsUnique();
    }
}
