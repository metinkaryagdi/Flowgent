using BitirmeProject.SprintService.Domain.Entities;

namespace BitirmeProject.SprintService.Application.Abstractions;

public interface ISprintSummaryRepository
{
    Task<SprintSummary?> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default);
    Task AddAsync(SprintSummary summary, CancellationToken cancellationToken = default);
}
