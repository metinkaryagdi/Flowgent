using BitirmeProject.ProjectService.Domain.Entities;

namespace BitirmeProject.ProjectService.Application.Abstractions;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByMemberUserIdAsync(Guid userId, Guid? organizationId = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Project> Items, int TotalCount)> GetByMemberUserIdPagedAsync(
        Guid userId,
        Guid? organizationId,
        int page,
        int pageSize,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Project> Items, int TotalCount)> GetByOrganizationIdPagedAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Project> Items, int TotalCount)> GetAllPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsByKeyAsync(
        string key,
        Guid? organizationId = null,
        Guid? excludeProjectId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
    Task DeleteAsync(Project project, CancellationToken cancellationToken = default);
}
