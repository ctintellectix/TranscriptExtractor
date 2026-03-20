using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TranscriptExtractor.Api.Startup;

namespace TranscriptExtractor.Tests.Api;

public class DatabaseMigrationTests
{
    [Fact]
    public async Task ApplyAsync_SkipsMigrationInTestingEnvironment()
    {
        var runner = new RecordingMigrationRunner();
        var environment = new TestHostEnvironment("Testing");

        await DatabaseMigration.ApplyAsync(environment, runner, CancellationToken.None);

        Assert.False(runner.WasCalled);
    }

    [Fact]
    public async Task ApplyAsync_InvokesMigrationOutsideTestingEnvironment()
    {
        var runner = new RecordingMigrationRunner();
        var environment = new TestHostEnvironment("Development");

        await DatabaseMigration.ApplyAsync(environment, runner, CancellationToken.None);

        Assert.True(runner.WasCalled);
    }

    private sealed class RecordingMigrationRunner : IDatabaseMigrationRunner
    {
        public bool WasCalled { get; private set; }

        public Task MigrateAsync(CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "TranscriptExtractor.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
