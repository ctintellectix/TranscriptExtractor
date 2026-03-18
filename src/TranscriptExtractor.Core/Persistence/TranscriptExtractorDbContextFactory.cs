using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TranscriptExtractor.Core.Persistence;

public sealed class TranscriptExtractorDbContextFactory : IDesignTimeDbContextFactory<TranscriptExtractorDbContext>
{
    private readonly string _connectionString;

    public TranscriptExtractorDbContextFactory()
        : this("Host=localhost;Port=5432;Database=transcript_extractor;Username=postgres;Password=postgres")
    {
    }

    public TranscriptExtractorDbContextFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public TranscriptExtractorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TranscriptExtractorDbContext>();
        TranscriptExtractorDbContextConfigurator.ConfigurePostgres(optionsBuilder, _connectionString);
        return new TranscriptExtractorDbContext(optionsBuilder.Options);
    }
}
