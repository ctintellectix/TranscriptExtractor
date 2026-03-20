using Microsoft.AspNetCore.Mvc.RazorPages;
using TranscriptExtractor.Ui.Api;
using TranscriptExtractor.Ui.Pages;

namespace TranscriptExtractor.Tests.Ui;

public class IndexModelTests
{
    [Fact]
    public async Task OnGetAsync_PopulatesDashboardDataFromApi()
    {
        var summary = new DashboardSummaryModel
        {
            QueuedCount = 3,
            ProcessingCount = 1,
            CompletedCount = 9,
            FailedCount = 2,
            LatestTranscriptReceivedAt = new DateTimeOffset(2026, 3, 20, 12, 30, 0, TimeSpan.Zero),
            LatestJobCreatedAt = new DateTimeOffset(2026, 3, 20, 12, 31, 0, TimeSpan.Zero),
            LatestJobUpdatedAt = new DateTimeOffset(2026, 3, 20, 12, 32, 0, TimeSpan.Zero),
            LatestCompletedAt = new DateTimeOffset(2026, 3, 20, 12, 33, 0, TimeSpan.Zero),
            LatestFailedAt = new DateTimeOffset(2026, 3, 20, 12, 34, 0, TimeSpan.Zero)
        };

        var recent = new[]
        {
            new RecentTranscriptModel
            {
                TranscriptId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                JobId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                CaseNumber = "CASE-101",
                Interviewer = "Jordan",
                SourceType = "witness_interview",
                JobStatus = "Failed",
                FailureMessage = "Extraction failed.",
                TranscriptReceivedAt = new DateTimeOffset(2026, 3, 20, 12, 15, 0, TimeSpan.Zero),
                JobCreatedAt = new DateTimeOffset(2026, 3, 20, 12, 16, 0, TimeSpan.Zero),
                JobUpdatedAt = new DateTimeOffset(2026, 3, 20, 12, 17, 0, TimeSpan.Zero),
                JobStartedAt = new DateTimeOffset(2026, 3, 20, 12, 16, 30, TimeSpan.Zero),
                JobCompletedAt = new DateTimeOffset(2026, 3, 20, 12, 17, 0, TimeSpan.Zero),
                ActivityAt = new DateTimeOffset(2026, 3, 20, 12, 17, 0, TimeSpan.Zero)
            }
        };

        var workerHealth = new WorkerHealthModel
        {
            WorkerName = "worker-a",
            Status = "healthy",
            LastPollAt = new DateTimeOffset(2026, 3, 20, 12, 35, 0, TimeSpan.Zero),
            LastSuccessfulJobAt = new DateTimeOffset(2026, 3, 20, 12, 34, 0, TimeSpan.Zero),
            LastErrorAt = null,
            LastError = null
        };

        var model = new IndexModel(new FakeDashboardApiClient(summary, recent, workerHealth));

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(summary, model.Summary);
        Assert.Equal(recent, model.RecentTranscripts);
        Assert.Equal(workerHealth, model.WorkerHealth);
        Assert.Null(model.LoadErrorMessage);
    }

    [Fact]
    public async Task OnGetAsync_WhenApiThrows_SetsLoadErrorMessage()
    {
        var model = new IndexModel(new ThrowingDashboardApiClient());

        await model.OnGetAsync(CancellationToken.None);

        Assert.Contains("dashboard", model.LoadErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(model.RecentTranscripts);
        Assert.Equal(0, model.Summary.QueuedCount);
        Assert.Null(model.WorkerHealth.WorkerName);
    }

    private sealed class FakeDashboardApiClient(
        DashboardSummaryModel summary,
        IReadOnlyList<RecentTranscriptModel> recent,
        WorkerHealthModel workerHealth) : IDashboardApiClient
    {
        public Task<DashboardSummaryModel> GetDashboardSummaryAsync(CancellationToken cancellationToken) => Task.FromResult(summary);

        public Task<IReadOnlyList<RecentTranscriptModel>> GetRecentTranscriptsAsync(CancellationToken cancellationToken) => Task.FromResult(recent);

        public Task<WorkerHealthModel> GetWorkerHealthAsync(CancellationToken cancellationToken) => Task.FromResult(workerHealth);
    }

    private sealed class ThrowingDashboardApiClient : IDashboardApiClient
    {
        public Task<DashboardSummaryModel> GetDashboardSummaryAsync(CancellationToken cancellationToken) => throw new HttpRequestException("API unavailable.");

        public Task<IReadOnlyList<RecentTranscriptModel>> GetRecentTranscriptsAsync(CancellationToken cancellationToken) => throw new HttpRequestException("API unavailable.");

        public Task<WorkerHealthModel> GetWorkerHealthAsync(CancellationToken cancellationToken) => throw new HttpRequestException("API unavailable.");
    }
}
