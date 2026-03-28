namespace BitirmeProject.NotificationService.Api.Background;

public sealed class NotificationDeliveryMonitor
{
    private readonly object _sync = new();

    public DateTime? LastStartedAt { get; private set; }
    public DateTime? LastCompletedAt { get; private set; }
    public int LastDeliveredCount { get; private set; }
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

    public void RecordCompleted(DateTime completedAt, int deliveredCount, int failedCount)
    {
        lock (_sync)
        {
            LastCompletedAt = completedAt;
            LastDeliveredCount = deliveredCount;
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
