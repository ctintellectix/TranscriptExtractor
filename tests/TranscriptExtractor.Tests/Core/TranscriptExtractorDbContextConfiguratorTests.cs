using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Core;
using TranscriptExtractor.Core.Persistence;

namespace TranscriptExtractor.Tests.Core;

public class TranscriptExtractorDbContextConfiguratorTests
{
    [Fact]
    public void ConfigurePostgres_AddsNpgsqlProviderExtension()
    {
        var builder = new DbContextOptionsBuilder<TranscriptExtractorDbContext>();

        TranscriptExtractorDbContextConfigurator.ConfigurePostgres(
            builder,
            "Host=localhost;Port=5432;Database=transcript_extractor;Username=postgres;Password=postgres");

        Assert.Contains(
            builder.Options.Extensions,
            extension => extension.GetType().FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true);
    }
}
