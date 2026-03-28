using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shared.Common.Health;

public sealed class OutboxPublisherHealthCheck : IHealthCheck
{
    private readonly OutboxPublisherMonitor _monitor;

    public OutboxPublisherHealthCheck(OutboxPublisherMonitor monitor)
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
            ["lastProcessedCount"] = _monitor.LastProcessedCount,
            ["lastFailedCount"] = _monitor.LastFailedCount,
            ["lastError"] = _monitor.LastError
        };

        if (_monitor.LastCompletedAt is null)
            return Task.FromResult(HealthCheckResult.Degraded("Outbox worker has not completed a cycle yet.", data: data));

        if (_monitor.LastCompletedAt < DateTime.UtcNow.AddMinutes(-2))
            return Task.FromResult(HealthCheckResult.Unhealthy("Outbox worker appears stale.", data: data));

        if (!string.IsNullOrWhiteSpace(_monitor.LastError))
            return Task.FromResult(HealthCheckResult.Degraded("Outbox worker reported an error.", data: data));

        return Task.FromResult(HealthCheckResult.Healthy("Outbox worker is healthy.", data));
    }
}
