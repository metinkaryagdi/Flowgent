using BitirmeProject.SprintService.Application.ReadModels;

namespace BitirmeProject.SprintService.Application.Abstractions;

public interface ISprintIssueRepository
{
    Task<SprintIssue?> GetByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SprintIssue>> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SprintIssue>> GetBacklogByProjectIdAsync(Guid projectId, Guid organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(SprintIssue sprintIssue, CancellationToken cancellationToken = default);
    Task UpdateAsync(SprintIssue sprintIssue, CancellationToken cancellationToken = default);
}
