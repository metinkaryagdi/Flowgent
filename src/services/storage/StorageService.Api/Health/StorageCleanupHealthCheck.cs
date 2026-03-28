using BitirmeProject.StorageService.Api.Background;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BitirmeProject.StorageService.Api.Health;

public sealed class StorageCleanupHealthCheck : IHealthCheck
{
    private readonly StorageCleanupMonitor _monitor;

    public StorageCleanupHealthCheck(StorageCleanupMonitor monitor)
    {
        _monitor = monitor;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object?>
        {
            ["lastStartedAt"] = _monitor.LastStartedAt,
            ["lastCompletedAt"] = _monitor.LastCompletedAt,
            ["lastExpiredTempDeletes"] = _monitor.LastExpiredTempDeletes,
            ["lastOrphanTempDeletes"] = _monitor.LastOrphanTempDeletes,
            ["lastMissingBinaryCount"] = _monitor.LastMissingBinaryCount,
            ["lastError"] = _monitor.LastError
        };

        if (_monitor.LastCompletedAt is null)
            return Task.FromResult(HealthCheckResult.Degraded("Storage cleanup worker has not completed a cycle yet.", data: data));

        if (_monitor.LastCompletedAt < DateTime.UtcNow.AddMinutes(-45))
            return Task.FromResult(HealthCheckResult.Unhealthy("Storage cleanup worker appears stale.", data: data));

        if (!string.IsNullOrWhiteSpace(_monitor.LastError))
            return Task.FromResult(HealthCheckResult.Degraded("Storage cleanup worker reported an error.", data: data));

        return Task.FromResult(HealthCheckResult.Healthy("Storage cleanup worker is healthy.", data));
    }
}
