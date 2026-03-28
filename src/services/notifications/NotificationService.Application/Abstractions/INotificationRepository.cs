using BitirmeProject.NotificationService.Domain.Entities;

namespace BitirmeProject.NotificationService.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetPendingDeliveriesAsync(int batchSize, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Notification?> GetByExternalEventIdAsync(Guid externalEventId, CancellationToken cancellationToken = default);
    Task<int> GetFailedDeliveryCountAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
}
