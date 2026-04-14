using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Domain.Entities;
using BitirmeProject.SprintService.Domain.Enums;
using BitirmeProject.SprintService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.SprintService.Infrastructure.Repositories;

public sealed class SprintRepository : ISprintRepository
{
    private readonly SprintDbContext _dbContext;

    public SprintRepository(SprintDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sprints.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sprint?> GetActiveByProjectIdAsync(Guid projectId, Guid? callerOrgId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Sprints
            .Where(s => s.ProjectId == projectId && s.Status == SprintStatus.Active);
        if (callerOrgId.HasValue)
            query = query.Where(s => s.OrganizationId == null || s.OrganizationId == callerOrgId);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sprint>> GetByProjectIdAsync(Guid projectId, Guid? callerOrgId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Sprints.Where(s => s.ProjectId == projectId);
        if (callerOrgId.HasValue)
            query = query.Where(s => s.OrganizationId == null || s.OrganizationId == callerOrgId);
        return await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        await _dbContext.Sprints.AddAsync(sprint, cancellationToken);
    }

    public Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        _dbContext.Sprints.Update(sprint);
        return Task.CompletedTask;
    }
}
