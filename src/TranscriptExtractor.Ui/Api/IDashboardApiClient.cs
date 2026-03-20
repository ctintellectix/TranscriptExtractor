namespace TranscriptExtractor.Ui.Api;

public interface IDashboardApiClient
{
    Task<DashboardSummaryModel> GetDashboardSummaryAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<RecentTranscriptModel>> GetRecentTranscriptsAsync(CancellationToken cancellationToken);
    Task<WorkerHealthModel> GetWorkerHealthAsync(CancellationToken cancellationToken);
}
