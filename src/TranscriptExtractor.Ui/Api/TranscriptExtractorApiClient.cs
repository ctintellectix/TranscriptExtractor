using System.Net.Http.Json;

namespace TranscriptExtractor.Ui.Api;

public sealed class TranscriptExtractorApiClient(HttpClient httpClient) : IDashboardApiClient
{
    public async Task<DashboardSummaryModel> GetDashboardSummaryAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<DashboardSummaryModel>("dashboard/summary", cancellationToken)
            ?? throw new InvalidOperationException("Dashboard summary response was empty.");
    }

    public async Task<IReadOnlyList<RecentTranscriptModel>> GetRecentTranscriptsAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<List<RecentTranscriptModel>>("dashboard/recent", cancellationToken)
            ?? throw new InvalidOperationException("Recent dashboard response was empty.");
    }

    public async Task<WorkerHealthModel> GetWorkerHealthAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<WorkerHealthModel>("worker/health", cancellationToken)
            ?? throw new InvalidOperationException("Worker health response was empty.");
    }
}
