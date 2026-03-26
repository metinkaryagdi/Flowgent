using BitirmeProject.ProjectService.Domain.Entities;

namespace BitirmeProject.ProjectService.Application.Abstractions;

public interface IProjectSummaryRepository
{
    Task<ProjectSummary?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectSummary>> GetByProjectIdsAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken = default);
    Task AddAsync(ProjectSummary summary, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProjectSummary summary, CancellationToken cancellationToken = default);
}
