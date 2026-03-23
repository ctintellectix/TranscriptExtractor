using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Core;

namespace TranscriptExtractor.Api.Startup;

public interface IDatabaseMigrationRunner
{
    Task MigrateAsync(CancellationToken cancellationToken);
}

public sealed class DatabaseMigrationRunner(IServiceProvider serviceProvider) : IDatabaseMigrationRunner
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}

public static class DatabaseMigration
{
    public static async Task ApplyAsync(IHostEnvironment environment, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var migrationRunner = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationRunner>();
        await migrationRunner.MigrateAsync(cancellationToken);
    }
}
