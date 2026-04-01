using BitirmeProject.ProjectService.Domain.Entities;

namespace BitirmeProject.ProjectService.Application.Abstractions;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Project> Items, int TotalCount)> GetByMemberUserIdPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
}
