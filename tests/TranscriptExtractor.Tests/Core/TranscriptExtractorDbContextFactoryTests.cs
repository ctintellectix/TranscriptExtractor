using TranscriptExtractor.Core.Persistence;

namespace TranscriptExtractor.Tests.Core;

public class TranscriptExtractorDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_UsesProvidedConnectionString()
    {
        var factory = new TranscriptExtractorDbContextFactory(
            "Host=localhost;Port=5432;Database=transcript_extractor;Username=postgres;Password=postgres");

        using var context = factory.CreateDbContext([]);

        Assert.Contains(
            "Npgsql",
            context.Database.ProviderName ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }
}
