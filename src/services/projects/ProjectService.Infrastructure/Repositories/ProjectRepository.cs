using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Domain.Entities;
using BitirmeProject.ProjectService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.ProjectService.Infrastructure.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly ProjectDbContext _dbContext;

    public ProjectRepository(ProjectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim().ToUpperInvariant();
        return await _dbContext.Projects.AnyAsync(p => p.Key == normalized, cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _dbContext.Projects.AddAsync(project, cancellationToken);
    }

    public Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        _dbContext.Projects.Update(project);
        return Task.CompletedTask;
    }
}