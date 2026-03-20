namespace TranscriptExtractor.Core.Entities;

public class WorkerHeartbeat
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string WorkerName { get; private set; } = string.Empty;
    public DateTimeOffset LastPollAt { get; set; }
    public DateTimeOffset? LastSuccessfulJobAt { get; set; }
    public DateTimeOffset? LastErrorAt { get; set; }
    public string? LastError { get; set; }

    public WorkerHeartbeat(string workerName)
    {
        if (string.IsNullOrWhiteSpace(workerName))
        {
            throw new ArgumentException("Worker name cannot be blank.", nameof(workerName));
        }

        WorkerName = workerName;
    }

    private WorkerHeartbeat()
    {
    }
}
