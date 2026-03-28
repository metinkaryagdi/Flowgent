using AutoMapper;
using BitirmeProject.NotificationService.Api.Hubs;
using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.NotificationService.Api.Background;

public sealed class NotificationDeliveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly NotificationDeliveryMonitor _monitor;
    private readonly ILogger<NotificationDeliveryWorker> _logger;
    private readonly TimeSpan _interval;
    private const int BatchSize = 50;

    public NotificationDeliveryWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<NotificationsHub> hubContext,
        NotificationDeliveryMonitor monitor,
        ILogger<NotificationDeliveryWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _monitor = monitor;
        _logger = logger;

        var intervalSeconds = configuration.GetValue<int?>("Notifications:DeliveryIntervalSeconds") ?? 5;
        _interval = TimeSpan.FromSeconds(Math.Max(2, intervalSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeliverNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _monitor.RecordFailure(DateTime.UtcNow, ex);
                _logger.LogError(ex, "Notification delivery worker failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task DeliverNotificationsAsync(CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;
        _monitor.RecordStarted(startedAt);

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var notifications = await repository.GetPendingDeliveriesAsync(BatchSize, DateTime.UtcNow, cancellationToken);
        var deliveredCount = 0;
        var failedCount = 0;

        foreach (var notification in notifications)
        {
            notification.RegisterDeliveryAttempt();

            try
            {
                switch (notification.Channel)
                {
                    case NotificationChannel.InApp:
                    {
                        notification.MarkAsDelivered();
                        var dto = mapper.Map<NotificationDto>(notification);
                        await _hubContext.Clients
                            .Group($"user-{notification.UserId}")
                            .SendAsync("notification", dto, cancellationToken);
                        break;
                    }

                    case NotificationChannel.Email:
                        await emailSender.SendAsync(notification.UserId, notification.Title, notification.Message, cancellationToken);
                        notification.MarkAsSent();
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported notification channel: {notification.Channel}");
                }

                await repository.UpdateAsync(notification, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                deliveredCount++;
            }
            catch (Exception ex)
            {
                notification.MarkAsFailed(ex.Message, CalculateRetryDelay(notification.DeliveryAttemptCount));
                await repository.UpdateAsync(notification, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                failedCount++;
                _logger.LogError(
                    ex,
                    "Notification delivery failed. NotificationId={NotificationId}, UserId={UserId}, Channel={Channel}",
                    notification.Id,
                    notification.UserId,
                    notification.Channel);
            }
        }

        _monitor.RecordCompleted(DateTime.UtcNow, deliveredCount, failedCount);

        if (notifications.Count > 0)
        {
            _logger.LogInformation(
                "Notification delivery cycle completed. DeliveredCount={DeliveredCount}, FailedCount={FailedCount}",
                deliveredCount,
                failedCount);
        }
    }

    private static TimeSpan CalculateRetryDelay(int attemptCount)
    {
        var seconds = Math.Min(30 * attemptCount, 300);
        return TimeSpan.FromSeconds(Math.Max(30, seconds));
    }
}
