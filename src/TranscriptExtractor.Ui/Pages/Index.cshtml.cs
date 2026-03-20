using TranscriptExtractor.Ui.Api;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TranscriptExtractor.Ui.Pages;

public class IndexModel(IDashboardApiClient dashboardApiClient) : PageModel
{
    public DashboardSummaryModel Summary { get; private set; } = new();
    public IReadOnlyList<RecentTranscriptModel> RecentTranscripts { get; private set; } = Array.Empty<RecentTranscriptModel>();
    public WorkerHealthModel WorkerHealth { get; private set; } = new();
    public string? LoadErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var summaryTask = dashboardApiClient.GetDashboardSummaryAsync(cancellationToken);
            var recentTask = dashboardApiClient.GetRecentTranscriptsAsync(cancellationToken);
            var workerHealthTask = dashboardApiClient.GetWorkerHealthAsync(cancellationToken);

            await Task.WhenAll(summaryTask, recentTask, workerHealthTask);

            Summary = await summaryTask;
            RecentTranscripts = await recentTask;
            WorkerHealth = await workerHealthTask;
        }
        catch (HttpRequestException)
        {
            LoadErrorMessage = "Unable to load dashboard data right now. Check the API connection.";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LoadErrorMessage = "Unable to load dashboard data right now. Check the API connection.";
        }
    }
}
