using Microsoft.EntityFrameworkCore;

namespace TranscriptExtractor.Core.Persistence;

public static class TranscriptExtractorDbContextConfigurator
{
    public static void ConfigurePostgres(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        optionsBuilder.UseNpgsql(connectionString);
    }
}
