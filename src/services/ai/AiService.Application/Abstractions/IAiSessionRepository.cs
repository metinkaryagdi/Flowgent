using BitirmeProject.AiService.Domain.Entities;

namespace BitirmeProject.AiService.Application.Abstractions;

public interface IAiSessionRepository
{
    Task<AiSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<AiSession>> GetByProjectAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task AddAsync(AiSession session, CancellationToken ct = default);
    Task AddResultAsync(AiPlanResult result, CancellationToken ct = default);
    Task UpdateAsync(AiSession session, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
