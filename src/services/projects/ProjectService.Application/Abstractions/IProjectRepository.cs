using BitirmeProject.ProjectService.Domain.Entities;

namespace BitirmeProject.ProjectService.Application.Abstractions;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
}