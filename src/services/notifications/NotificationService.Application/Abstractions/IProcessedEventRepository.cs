using BitirmeProject.NotificationService.Domain.Entities;

namespace BitirmeProject.NotificationService.Application.Abstractions;

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default);
}
