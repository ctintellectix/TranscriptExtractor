using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TranscriptExtractor.Core.Extraction;
using WorkerService = TranscriptExtractor.Worker.Worker;

namespace TranscriptExtractor.Tests.Worker;

public class WorkerLifetimeTests
{
    [Fact]
    public void Worker_CanBeCreatedFromSingletonScopeUsingScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new TranscriptExtractionOrchestrator(
            db: null!,
            promptAssetLoader: null!,
            extractionClient: null!,
            promptDirectory: "unused"));
        services.AddSingleton(new TranscriptExtractor.Worker.WorkerIdentity("worker-a"));

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var worker = new WorkerService(
            NullLogger<WorkerService>.Instance,
            scopeFactory,
            provider.GetRequiredService<TranscriptExtractor.Worker.WorkerIdentity>());

        Assert.NotNull(worker);
    }
}
