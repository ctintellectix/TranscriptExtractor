using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TranscriptExtractor.Core;
using TranscriptExtractor.Core.Entities;

namespace TranscriptExtractor.Tests.Api;

public class DashboardEndpointsTests : IClassFixture<TranscriptApiFactory>
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(15);

    private readonly TranscriptApiFactory _factory;
    private readonly HttpClient _client;

    public DashboardEndpointsTests(TranscriptApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDashboardSummary_ReturnsCountsAndLatestTimestamps()
    {
        await _factory.ResetDatabaseAsync();

        var now = DateTimeOffset.UtcNow;
        await SeedTranscriptJobAsync("queued", ExtractionJobStatus.Queued, now.AddMinutes(-40), now.AddMinutes(-35), now.AddMinutes(-35));
        await SeedTranscriptJobAsync("processing", ExtractionJobStatus.Processing, now.AddMinutes(-30), now.AddMinutes(-20), now.AddMinutes(-12));
        await SeedTranscriptJobAsync("completed", ExtractionJobStatus.Completed, now.AddMinutes(-18), now.AddMinutes(-10), now.AddMinutes(-8), now.AddMinutes(-8));
        await SeedTranscriptJobAsync("failed", ExtractionJobStatus.Failed, now.AddMinutes(-14), now.AddMinutes(-6), now.AddMinutes(-2), now.AddMinutes(-2), error: "Extraction failed.");

        var response = await _client.GetAsync("/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, payload.RootElement.GetProperty("queuedCount").GetInt32());
        Assert.Equal(1, payload.RootElement.GetProperty("processingCount").GetInt32());
        Assert.Equal(1, payload.RootElement.GetProperty("completedCount").GetInt32());
        Assert.Equal(1, payload.RootElement.GetProperty("failedCount").GetInt32());
        Assert.Equal(now.AddMinutes(-14), payload.RootElement.GetProperty("latestTranscriptReceivedAt").GetDateTimeOffset());
        Assert.Equal(now.AddMinutes(-6), payload.RootElement.GetProperty("latestJobCreatedAt").GetDateTimeOffset());
        Assert.Equal(now.AddMinutes(-2), payload.RootElement.GetProperty("latestJobUpdatedAt").GetDateTimeOffset());
        Assert.Equal(now.AddMinutes(-8), payload.RootElement.GetProperty("latestCompletedAt").GetDateTimeOffset());
        Assert.Equal(now.AddMinutes(-2), payload.RootElement.GetProperty("latestFailedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task GetDashboardRecent_ReturnsRecentActivityInDescendingOrder()
    {
        await _factory.ResetDatabaseAsync();

        var now = DateTimeOffset.UtcNow;
        var sharedTranscript = await SeedTranscriptAsync("shared", Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"));
        await SeedJobAsync(sharedTranscript, "shared-queued", ExtractionJobStatus.Queued, now.AddMinutes(-40), now.AddMinutes(-35), now.AddMinutes(-35));
        var sharedLatestJob = await SeedJobAsync(sharedTranscript, "shared-failed", ExtractionJobStatus.Failed, now.AddMinutes(-14), now.AddMinutes(-4), now.AddMinutes(-1), now.AddMinutes(-1), error: "Extraction failed.");

        var otherTranscript = await SeedTranscriptAsync("other", Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var otherLatestJob = await SeedJobAsync(otherTranscript, "other-completed", ExtractionJobStatus.Completed, now.AddMinutes(-14), now.AddMinutes(-4), now.AddMinutes(-1), completedAt: now.AddMinutes(-1));

        var response = await _client.GetAsync("/dashboard/recent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = payload.RootElement.EnumerateArray().ToArray();

        Assert.Equal(2, items.Length);
        Assert.Equal(sharedTranscript.Id, items[0].GetProperty("transcriptId").GetGuid());
        Assert.Equal(otherTranscript.Id, items[1].GetProperty("transcriptId").GetGuid());
        Assert.Equal("Failed", items[0].GetProperty("jobStatus").GetString());
        Assert.Equal("shared", items[0].GetProperty("interviewer").GetString());
        Assert.Equal("Extraction failed.", items[0].GetProperty("failureMessage").GetString());
        Assert.Equal(sharedLatestJob.Id, items[0].GetProperty("jobId").GetGuid());
        Assert.Equal("Completed", items[1].GetProperty("jobStatus").GetString());
        Assert.Equal("other", items[1].GetProperty("interviewer").GetString());
        Assert.Null(items[1].GetProperty("failureMessage").GetString());
        Assert.Equal(otherLatestJob.Id, items[1].GetProperty("jobId").GetGuid());
    }

    [Fact]
    public async Task GetWorkerHealth_ReturnsIdleWhenNewerErrorFollowsSuccess()
    {
        await _factory.ResetDatabaseAsync();

        var heartbeat = await SeedHeartbeatAsync("worker-d", DateTimeOffset.UtcNow.AddMinutes(-1));
        await UpdateHeartbeatAsync(
            heartbeat,
            lastSuccessfulJobAt: DateTimeOffset.UtcNow.AddMinutes(-2),
            lastErrorAt: DateTimeOffset.UtcNow.AddSeconds(-30),
            lastError: "Extraction failed.");

        var response = await _client.GetAsync("/worker/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("idle", payload.RootElement.GetProperty("status").GetString());
        Assert.Equal("worker-d", payload.RootElement.GetProperty("workerName").GetString());
        Assert.Equal("Extraction failed.", payload.RootElement.GetProperty("lastError").GetString());
    }

    [Fact]
    public async Task GetWorkerHealth_ReturnsOfflineWhenNoHeartbeatExists()
    {
        await _factory.ResetDatabaseAsync();

        var response = await _client.GetAsync("/worker/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("offline", payload.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetWorkerHealth_ReturnsIdleWhenHeartbeatIsFreshAndNoSuccessExists()
    {
        await _factory.ResetDatabaseAsync();

        await SeedHeartbeatAsync("worker-a", DateTimeOffset.UtcNow.AddMinutes(-1));

        var response = await _client.GetAsync("/worker/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("idle", payload.RootElement.GetProperty("status").GetString());
        Assert.Equal("worker-a", payload.RootElement.GetProperty("workerName").GetString());
    }

    [Fact]
    public async Task GetWorkerHealth_ReturnsHealthyWhenHeartbeatIsFreshAndSuccessExists()
    {
        await _factory.ResetDatabaseAsync();

        var heartbeat = await SeedHeartbeatAsync("worker-b", DateTimeOffset.UtcNow.AddMinutes(-1));
        await UpdateHeartbeatAsync(heartbeat, lastSuccessfulJobAt: DateTimeOffset.UtcNow.AddMinutes(-2));

        var response = await _client.GetAsync("/worker/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("healthy", payload.RootElement.GetProperty("status").GetString());
        Assert.Equal("worker-b", payload.RootElement.GetProperty("workerName").GetString());
    }

    [Fact]
    public async Task GetWorkerHealth_ReturnsStaleWhenHeartbeatIsOlderThanThreshold()
    {
        await _factory.ResetDatabaseAsync();

        await SeedHeartbeatAsync("worker-c", DateTimeOffset.UtcNow.Subtract(StaleThreshold).AddMinutes(-1));

        var response = await _client.GetAsync("/worker/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("stale", payload.RootElement.GetProperty("status").GetString());
        Assert.Equal("worker-c", payload.RootElement.GetProperty("workerName").GetString());
    }

    private async Task<(Guid TranscriptId, Guid JobId)> SeedTranscriptJobAsync(
        string label,
        ExtractionJobStatus status,
        DateTimeOffset receivedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? completedAt = null,
        string? error = null)
    {
        var transcript = await SeedTranscriptAsync(label);
        var job = await SeedJobAsync(transcript, label, status, receivedAt, createdAt, updatedAt, completedAt, error);
        return (transcript.Id, job.Id);
    }

    private async Task<Transcript> SeedTranscriptAsync(string label, Guid? transcriptId = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();

        var transcript = new Transcript
        {
            Id = transcriptId ?? Guid.NewGuid(),
            TranscriptText = $"Transcript {label}",
            SourceType = "witness_interview",
            CaseNumber = $"CASE-{label.ToUpperInvariant()}",
            Interviewer = label
        };

        db.Transcripts.Add(transcript);
        await db.SaveChangesAsync();
        return transcript;
    }

    private async Task<ExtractionJob> SeedJobAsync(
        Transcript transcript,
        string label,
        ExtractionJobStatus status,
        DateTimeOffset receivedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? completedAt = null,
        string? error = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();

        var trackedTranscript = await db.Transcripts.SingleAsync(x => x.Id == transcript.Id);
        db.Entry(trackedTranscript).Property(x => x.ReceivedAt).CurrentValue = receivedAt;

        var job = new ExtractionJob(trackedTranscript.Id);
        db.ExtractionJobs.Add(job);

        db.Entry(job).Property(x => x.CreatedAt).CurrentValue = createdAt;
        db.Entry(job).Property(x => x.UpdatedAt).CurrentValue = updatedAt;
        db.Entry(job).Property(x => x.Status).CurrentValue = status;

        if (status == ExtractionJobStatus.Processing)
        {
            db.Entry(job).Property(x => x.StartedAt).CurrentValue = updatedAt.AddMinutes(-1);
        }

        if (status == ExtractionJobStatus.Completed)
        {
            db.Entry(job).Property(x => x.CompletedAt).CurrentValue = completedAt ?? updatedAt;
            db.Entry(job).Property(x => x.Model).CurrentValue = "gpt-5.4-mini";
            db.Entry(job).Property(x => x.PromptVersion).CurrentValue = "lvpd-v1";
        }

        if (status == ExtractionJobStatus.Failed)
        {
            db.Entry(job).Property(x => x.CompletedAt).CurrentValue = completedAt ?? updatedAt;
            db.Entry(job).Property(x => x.Error).CurrentValue = error ?? $"Job {label} failed.";
        }

        await db.SaveChangesAsync();
        return job;
    }

    private async Task<WorkerHeartbeat> SeedHeartbeatAsync(string workerName, DateTimeOffset lastPollAt)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();

        var heartbeat = new WorkerHeartbeat(workerName);
        db.WorkerHeartbeats.Add(heartbeat);
        db.Entry(heartbeat).Property(x => x.LastPollAt).CurrentValue = lastPollAt;

        await db.SaveChangesAsync();
        return heartbeat;
    }

    private async Task UpdateHeartbeatAsync(WorkerHeartbeat heartbeat, DateTimeOffset? lastSuccessfulJobAt = null, DateTimeOffset? lastErrorAt = null, string? lastError = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranscriptExtractorDbContext>();

        var tracked = await db.WorkerHeartbeats.SingleAsync(x => x.Id == heartbeat.Id);
        if (lastSuccessfulJobAt.HasValue)
        {
            tracked.LastSuccessfulJobAt = lastSuccessfulJobAt;
        }

        if (lastErrorAt.HasValue)
        {
            tracked.LastErrorAt = lastErrorAt;
        }

        if (lastError is not null)
        {
            tracked.LastError = lastError;
        }

        await db.SaveChangesAsync();
    }
}
