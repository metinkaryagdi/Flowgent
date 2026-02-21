using BitirmeProject.SprintService.Domain.Entities;

namespace BitirmeProject.SprintService.Application.Abstractions;

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default);
}
