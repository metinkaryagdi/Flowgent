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

    public async Task<IReadOnlyList<Project>> GetByMemberUserIdAsync(Guid userId, Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Projects
            .Where(p => _dbContext.ProjectMembers.Any(m => m.ProjectId == p.Id && m.UserId == userId));

        if (organizationId.HasValue)
        {
            query = query.Where(p => !p.OrganizationId.HasValue || p.OrganizationId == organizationId);
        }

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Project> Items, int TotalCount)> GetByMemberUserIdPagedAsync(
        Guid userId,
        Guid? organizationId,
        int page,
        int pageSize,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Projects
            .Where(p => _dbContext.ProjectMembers.Any(m => m.ProjectId == p.Id && m.UserId == userId));

        if (organizationId.HasValue)
        {
            query = query.Where(p => !p.OrganizationId.HasValue || p.OrganizationId == organizationId);
        }

        if (!includeArchived)
        {
            query = query.Where(p => !p.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Key.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Project>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .Where(p => p.OrganizationId == organizationId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Project> Items, int TotalCount)> GetByOrganizationIdPagedAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Projects.Where(p => p.OrganizationId == organizationId);

        if (!includeArchived)
            query = query.Where(p => !p.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Key.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsByKeyAsync(
        string key,
        Guid? organizationId = null,
        Guid? excludeProjectId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim().ToUpperInvariant();
        var query = _dbContext.Projects.Where(p => p.Key == normalized);

        if (organizationId.HasValue)
        {
            query = query.Where(p => p.OrganizationId == organizationId);
        }
        else
        {
            query = query.Where(p => p.OrganizationId == null);
        }

        if (excludeProjectId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProjectId.Value);
        }

        return await query.AnyAsync(cancellationToken);
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
