using BitirmeProject.IssueService.Domain.Entities;

namespace BitirmeProject.IssueService.Application.Abstractions;

public interface IIssueRepository
{
    Task<Issue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issue>> GetByAssigneeAsync(Guid assigneeUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issue>> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default);
    Task AddAsync(Issue issue, CancellationToken cancellationToken = default);
    Task UpdateAsync(Issue issue, CancellationToken cancellationToken = default);
}
