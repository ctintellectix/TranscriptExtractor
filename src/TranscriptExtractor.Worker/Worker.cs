using TranscriptExtractor.Core.Extraction;

namespace TranscriptExtractor.Worker;

public class Worker(
    ILogger<Worker> logger,
    TranscriptExtractionOrchestrator orchestrator) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = await orchestrator.ProcessOneAsync(stoppingToken);

            if (!processed && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("No queued extraction jobs at: {time}", DateTimeOffset.Now);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}
