using BitirmeProject.IssueService.Application.ReadModels;

namespace BitirmeProject.IssueService.Application.Abstractions;

public interface IIssueBoardRepository
{
    Task<IssueBoardItem?> GetByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IssueBoardItem>> GetByIssueIdsAsync(IReadOnlyCollection<Guid> issueIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IssueBoardItem>> GetByProjectIdAsync(Guid projectId, Guid? callerOrgId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IssueBoardItem>> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<IssueBoardItem> Items, int TotalCount)> GetByProjectIdPagedAsync(
        Guid projectId,
        int page,
        int pageSize,
        Guid? sprintId = null,
        bool backlogOnly = false,
        Guid? callerOrgId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(IssueBoardItem boardItem, CancellationToken cancellationToken = default);
    Task RemoveByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default);
    Task UpdateAsync(IssueBoardItem boardItem, CancellationToken cancellationToken = default);
}
