using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Core.Extraction;
using TranscriptExtractor.Core.Entities;
using TranscriptExtractor.Core;
using System.Data.Common;
using System.Net.Sockets;

namespace TranscriptExtractor.Worker;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory,
    WorkerIdentity workerIdentity) : BackgroundService
{
    private static readonly TimeSpan NormalDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromSeconds(5);

    private TimeSpan _databaseRetryDelay;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteCycleWithRetryAsync(stoppingToken);
        }
    }

    protected virtual async Task ExecuteCycleWithRetryAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ExecuteCycleAsync(stoppingToken);

            if (_databaseRetryDelay != TimeSpan.Zero)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Worker {workerName} recovered database connectivity.",
                        workerIdentity.WorkerName);
                }

                _databaseRetryDelay = TimeSpan.Zero;
            }

            await DelayAsync(NormalDelay, stoppingToken);
        }
        catch (Exception ex) when (IsDatabaseConnectivityException(ex))
        {
            _databaseRetryDelay = NextRetryDelay();

            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    ex,
                    "Worker {workerName} could not reach the database. Retrying in {delay}.",
                    workerIdentity.WorkerName,
                    _databaseRetryDelay);
            }

            await DelayAsync(_databaseRetryDelay, stoppingToken);
        }
    }

    protected virtual async Task ExecuteCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<TranscriptExtractionOrchestrator>();
        var heartbeat = await LoadHeartbeatAsync(db, stoppingToken);
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

        await SaveChangesAsync(db, stoppingToken);
    }

    protected virtual Task<WorkerHeartbeat?> LoadHeartbeatAsync(TranscriptExtractorDbContext db, CancellationToken stoppingToken)
    {
        return db.WorkerHeartbeats.SingleOrDefaultAsync(x => x.WorkerName == workerIdentity.WorkerName, stoppingToken);
    }

    protected virtual Task SaveChangesAsync(TranscriptExtractorDbContext db, CancellationToken stoppingToken)
    {
        return db.SaveChangesAsync(stoppingToken);
    }

    protected virtual Task DelayAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        return Task.Delay(delay, stoppingToken);
    }

    private TimeSpan NextRetryDelay()
    {
        if (_databaseRetryDelay == TimeSpan.Zero)
        {
            return InitialRetryDelay;
        }

        var doubled = TimeSpan.FromMilliseconds(_databaseRetryDelay.TotalMilliseconds * 2);
        return doubled <= MaximumRetryDelay ? doubled : MaximumRetryDelay;
    }

    private static bool IsDatabaseConnectivityException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException or DbException or SocketException)
            {
                return true;
            }

            var type = current.GetType();
            if (type.Namespace?.StartsWith("Npgsql", StringComparison.OrdinalIgnoreCase) == true ||
                type.FullName?.StartsWith("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        return false;
    }
}

public sealed record WorkerIdentity(string WorkerName);
