using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TranscriptExtractor.Core;
using TranscriptExtractor.Core.Entities;
using TranscriptExtractor.Core.Extraction;
using TranscriptExtractor.Core.Prompts;
using TranscriptExtractor.Worker;
using WorkerService = TranscriptExtractor.Worker.Worker;

namespace TranscriptExtractor.Tests.Worker;

public class WorkerHeartbeatTests
{
    [Fact]
    public async Task IdlePoll_UpdatesLastPollAt()
    {
        var workerName = "worker-heartbeat-idle";
        using var host = CreateHost(workerName, transcriptText: null, extractionClientFactory: _ => new IdleTranscriptExtractionClient());
        var worker = CreateWorker(host, workerName);

        await worker.RunOnceAsync(CancellationToken.None);

        using var queryScope = host.CreateScope();
        var db = queryScope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();
        db.ChangeTracker.Clear();

        var heartbeat = await db
            .WorkerHeartbeats
            .SingleAsync();

        Assert.Equal(workerName, heartbeat.WorkerName);
        Assert.NotEqual(default, heartbeat.LastPollAt);
        Assert.Null(heartbeat.LastSuccessfulJobAt);
        Assert.Null(heartbeat.LastErrorAt);
        Assert.Null(heartbeat.LastError);
    }

    [Fact]
    public async Task SuccessfulProcessingCycle_UpdatesLastSuccessfulJobAt()
    {
        var workerName = "worker-heartbeat-success";
        using var host = CreateHost(workerName, transcriptText: "Transcript text.", extractionClientFactory: _ => new SuccessfulTranscriptExtractionClient("""{ "people": [], "statements": [] }""", "gpt-5.4-mini"));
        var worker = CreateWorker(host, workerName);

        await worker.RunOnceAsync(CancellationToken.None);

        using var queryScope = host.CreateScope();
        var db = queryScope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();
        db.ChangeTracker.Clear();

        var heartbeat = await db
            .WorkerHeartbeats
            .SingleAsync();

        Assert.Equal(workerName, heartbeat.WorkerName);
        Assert.NotEqual(default, heartbeat.LastPollAt);
        Assert.NotNull(heartbeat.LastSuccessfulJobAt);
        Assert.Null(heartbeat.LastErrorAt);
        Assert.Null(heartbeat.LastError);
    }

    [Fact]
    public async Task UnexpectedProcessingFailure_RecordsLastError()
    {
        var workerName = "worker-heartbeat-failure";
        using var host = CreateHost(workerName, transcriptText: "Transcript text.", extractionClientFactory: _ => new ThrowingTranscriptExtractionClient("boom"));
        var worker = CreateWorker(host, workerName);

        await worker.RunOnceAsync(CancellationToken.None);

        using var queryScope = host.CreateScope();
        var db = queryScope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();
        db.ChangeTracker.Clear();

        var heartbeat = await db
            .WorkerHeartbeats
            .SingleAsync();

        Assert.Equal(workerName, heartbeat.WorkerName);
        Assert.NotEqual(default, heartbeat.LastPollAt);
        Assert.Null(heartbeat.LastSuccessfulJobAt);
        Assert.NotNull(heartbeat.LastErrorAt);
        Assert.Equal("boom", heartbeat.LastError);
    }

    private static TestWorker CreateWorker(IServiceProvider serviceProvider, string workerName)
    {
        return new TestWorker(
            NullLogger<WorkerService>.Instance,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            new WorkerIdentity(workerName));
    }

    private static ServiceProvider CreateHost(
        string workerName,
        string? transcriptText,
        Func<TranscriptExtractorDbContext, ITranscriptExtractionClient> extractionClientFactory)
    {
        var root = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString("n");
        var services = new ServiceCollection();
        services.AddDbContext<TranscriptExtractorDbContext>(options =>
            options.UseInMemoryDatabase(databaseName, root));
        services.AddSingleton(new WorkerIdentity(workerName));
        services.AddSingleton<IPromptAssetLoader>(_ => new FakePromptAssetLoader(new PromptAssets("lvpd-v1", "System", "Prompt <<<TRANSCRIPT_TEXT>>> <<<JSON OUTPUT SCHEMA>>>", "{ }")));
        services.AddScoped<ITranscriptExtractionClient>(sp =>
            extractionClientFactory(sp.GetRequiredService<TranscriptExtractorDbContext>()));
        services.AddScoped<TranscriptExtractionOrchestrator>(sp =>
            new TranscriptExtractionOrchestrator(
                sp.GetRequiredService<TranscriptExtractorDbContext>(),
                sp.GetRequiredService<IPromptAssetLoader>(),
                sp.GetRequiredService<ITranscriptExtractionClient>(),
                promptDirectory: "unused"));

        var provider = services.BuildServiceProvider(validateScopes: true);

        if (transcriptText is not null)
        {
            using var seedScope = provider.CreateScope();
            var db = seedScope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();
            var transcript = new Transcript
            {
                TranscriptText = transcriptText,
                SourceType = "witness_interview"
            };
            db.Transcripts.Add(transcript);
            db.ExtractionJobs.Add(new ExtractionJob(transcript.Id));
            db.SaveChanges();
        }

        return provider;
    }

    private sealed class TestWorker(
        ILogger<WorkerService> logger,
        IServiceScopeFactory scopeFactory,
        WorkerIdentity identity) : WorkerService(logger, scopeFactory, identity)
    {
        public Task RunOnceAsync(CancellationToken cancellationToken) => ExecuteCycleAsync(cancellationToken);
    }

    private sealed class FakePromptAssetLoader(PromptAssets assets) : IPromptAssetLoader
    {
        public PromptAssets Load(string promptDirectory) => assets;
    }

    private sealed class IdleTranscriptExtractionClient : ITranscriptExtractionClient
    {
        public Task<TranscriptExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("No extraction should be requested for an idle cycle.");
        }
    }

    private sealed class SuccessfulTranscriptExtractionClient(string json, string model) : ITranscriptExtractionClient
    {
        public Task<TranscriptExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TranscriptExtractionResult(json, model));
        }
    }

    private sealed class ThrowingTranscriptExtractionClient(string message) : ITranscriptExtractionClient
    {
        public Task<TranscriptExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(message);
        }
    }
}
