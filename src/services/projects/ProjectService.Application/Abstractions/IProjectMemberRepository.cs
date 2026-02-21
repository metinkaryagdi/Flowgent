using BitirmeProject.ProjectService.Domain.Entities;

namespace BitirmeProject.ProjectService.Application.Abstractions;

public interface IProjectMemberRepository
{
    Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMember>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(ProjectMember member, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
}
