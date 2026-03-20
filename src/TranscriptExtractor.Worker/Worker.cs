using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Core.Extraction;
using TranscriptExtractor.Core.Entities;
using TranscriptExtractor.Core;

namespace TranscriptExtractor.Worker;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory,
    WorkerIdentity workerIdentity) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteCycleAsync(stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }

    protected virtual async Task ExecuteCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<TranscriptExtractionOrchestrator>();
        var heartbeat = await db.WorkerHeartbeats.SingleOrDefaultAsync(x => x.WorkerName == workerIdentity.WorkerName, stoppingToken);
        var pollAt = DateTimeOffset.UtcNow;

        if (heartbeat is null)
        {
            heartbeat = new WorkerHeartbeat(workerIdentity.WorkerName);
            db.WorkerHeartbeats.Add(heartbeat);
        }

        heartbeat.LastPollAt = pollAt;

        try
        {
            var result = await orchestrator.ProcessOneWithResultAsync(stoppingToken);
            var completedAt = DateTimeOffset.UtcNow;

            if (result.Processed)
            {
                if (result.Succeeded)
                {
                    heartbeat.LastSuccessfulJobAt = completedAt;
                    heartbeat.LastErrorAt = null;
                    heartbeat.LastError = null;
                }
                else
                {
                    heartbeat.LastErrorAt = completedAt;
                    heartbeat.LastError = result.ErrorMessage;
                }
            }

            if (!result.Processed && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "No queued extraction jobs for {workerName} at: {time}",
                    workerIdentity.WorkerName,
                    DateTimeOffset.Now);
            }
        }
        catch (Exception ex)
        {
            heartbeat.LastErrorAt = DateTimeOffset.UtcNow;
            heartbeat.LastError = ex.Message;

            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Worker {workerName} failed while processing extraction jobs.", workerIdentity.WorkerName);
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }
}

public sealed record WorkerIdentity(string WorkerName);
