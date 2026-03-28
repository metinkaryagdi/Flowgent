using BitirmeProject.NotificationService.Api.Background;
using BitirmeProject.NotificationService.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BitirmeProject.NotificationService.Api.Health;

public sealed class NotificationDeliveryHealthCheck : IHealthCheck
{
    private readonly NotificationDeliveryMonitor _monitor;
    private readonly IServiceScopeFactory _scopeFactory;

    public NotificationDeliveryHealthCheck(
        NotificationDeliveryMonitor monitor,
        IServiceScopeFactory scopeFactory)
    {
        _monitor = monitor;
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var failedDeliveryCount = await repository.GetFailedDeliveryCountAsync(cancellationToken);

        var data = new Dictionary<string, object?>
        {
            ["lastStartedAt"] = _monitor.LastStartedAt,
            ["lastCompletedAt"] = _monitor.LastCompletedAt,
            ["lastDeliveredCount"] = _monitor.LastDeliveredCount,
            ["lastFailedCount"] = _monitor.LastFailedCount,
            ["failedDeliveryCount"] = failedDeliveryCount,
            ["lastError"] = _monitor.LastError
        };

        if (_monitor.LastCompletedAt is null)
            return HealthCheckResult.Degraded("Notification delivery worker has not completed a cycle yet.", data: data);

        if (_monitor.LastCompletedAt < DateTime.UtcNow.AddMinutes(-10))
            return HealthCheckResult.Unhealthy("Notification delivery worker appears stale.", data: data);

        if (!string.IsNullOrWhiteSpace(_monitor.LastError) || failedDeliveryCount > 0)
            return HealthCheckResult.Degraded("Notification delivery has failures requiring attention.", data: data);

        return HealthCheckResult.Healthy("Notification delivery worker is healthy.", data);
    }
}
