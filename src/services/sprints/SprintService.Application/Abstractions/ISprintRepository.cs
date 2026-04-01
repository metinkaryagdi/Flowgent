using BitirmeProject.SprintService.Domain.Entities;

namespace BitirmeProject.SprintService.Application.Abstractions;

public interface ISprintRepository
{
    Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sprint?> GetActiveByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sprint>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(Sprint sprint, CancellationToken cancellationToken = default);
    Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default);
}
