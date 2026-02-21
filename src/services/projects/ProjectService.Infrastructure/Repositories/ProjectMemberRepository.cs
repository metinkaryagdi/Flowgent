using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Domain.Entities;
using BitirmeProject.ProjectService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.ProjectService.Infrastructure.Repositories;

public sealed class ProjectMemberRepository : IProjectMemberRepository
{
    private readonly ProjectDbContext _dbContext;

    public ProjectMemberRepository(ProjectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProjectMembers.AnyAsync(x => x.ProjectId == projectId && x.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectMember>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProjectMembers
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.AddedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProjectMember member, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProjectMembers.AddAsync(member, cancellationToken);
    }

    public async Task RemoveAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ProjectMembers.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.UserId == userId, cancellationToken);
        if (existing is not null)
            _dbContext.ProjectMembers.Remove(existing);
    }
}
