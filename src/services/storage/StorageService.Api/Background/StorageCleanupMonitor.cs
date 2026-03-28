namespace BitirmeProject.StorageService.Api.Background;

public sealed class StorageCleanupMonitor
{
    private readonly object _sync = new();

    public DateTime? LastStartedAt { get; private set; }
    public DateTime? LastCompletedAt { get; private set; }
    public int LastExpiredTempDeletes { get; private set; }
    public int LastOrphanTempDeletes { get; private set; }
    public int LastMissingBinaryCount { get; private set; }
    public string? LastError { get; private set; }

    public void RecordStarted(DateTime startedAt)
    {
        lock (_sync)
        {
            LastStartedAt = startedAt;
            LastError = null;
        }
    }

    public void RecordCompleted(
        DateTime completedAt,
        int expiredTempDeletes,
        int orphanTempDeletes,
        int missingBinaryCount)
    {
        lock (_sync)
        {
            LastCompletedAt = completedAt;
            LastExpiredTempDeletes = expiredTempDeletes;
            LastOrphanTempDeletes = orphanTempDeletes;
            LastMissingBinaryCount = missingBinaryCount;
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
