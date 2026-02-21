using BitirmeProject.IssueService.Domain.Entities;

namespace BitirmeProject.IssueService.Application.Abstractions;

public interface IIssueBoardRepository
{
    Task<IssueBoardItem?> GetByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IssueBoardItem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(IssueBoardItem boardItem, CancellationToken cancellationToken = default);
    Task UpdateAsync(IssueBoardItem boardItem, CancellationToken cancellationToken = default);
}
