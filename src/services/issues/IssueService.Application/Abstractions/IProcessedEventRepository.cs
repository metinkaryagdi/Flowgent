using BitirmeProject.IssueService.Domain.Entities;

namespace BitirmeProject.IssueService.Application.Abstractions;

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default);
}
