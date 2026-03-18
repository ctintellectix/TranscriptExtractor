using TranscriptExtractor.Core.Extraction;

namespace TranscriptExtractor.Worker;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<TranscriptExtractionOrchestrator>();
            var processed = await orchestrator.ProcessOneAsync(stoppingToken);

            if (!processed && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("No queued extraction jobs at: {time}", DateTimeOffset.Now);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}
