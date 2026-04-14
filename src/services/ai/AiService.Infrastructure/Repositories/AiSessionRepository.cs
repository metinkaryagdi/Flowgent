using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.AiService.Infrastructure.Repositories;

public sealed class AiSessionRepository : IAiSessionRepository
{
    private readonly AiDbContext _db;

    public AiSessionRepository(AiDbContext db)
    {
        _db = db;
    }

    public async Task<AiSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.AiSessions.Include(s => s.Results).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<List<AiSession>> GetByProjectAsync(Guid projectId, Guid userId, CancellationToken ct = default)
        => await _db.AiSessions
            .Where(s => s.ProjectId == projectId && s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(AiSession session, CancellationToken ct = default)
        => await _db.AiSessions.AddAsync(session, ct);

    public Task UpdateAsync(AiSession session, CancellationToken ct = default)
    {
        _db.AiSessions.Update(session);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
