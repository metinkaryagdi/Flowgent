namespace Shared.Common.Health;

public sealed class OutboxPublisherMonitor
{
    private readonly object _sync = new();

    public DateTime? LastStartedAt { get; private set; }
    public DateTime? LastCompletedAt { get; private set; }
    public int LastProcessedCount { get; private set; }
    public int LastFailedCount { get; private set; }
    public string? LastError { get; private set; }

    public void RecordStarted(DateTime startedAt)
    {
        lock (_sync)
        {
            LastStartedAt = startedAt;
            LastError = null;
        }
    }

    public void RecordCompleted(DateTime completedAt, int processedCount, int failedCount)
    {
        lock (_sync)
        {
            LastCompletedAt = completedAt;
            LastProcessedCount = processedCount;
            LastFailedCount = failedCount;
            LastError = null;
        }
    }

    public void RecordFailure(DateTime failedAt, Exception exception)
    {
        lock (_sync)
        {
            LastCompletedAt = failedAt;
            LastError = exception.Message;
        }
    }
}
