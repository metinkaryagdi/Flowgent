using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Infrastructure.Persistence;

namespace BitirmeProject.AiService.Infrastructure.Repositories;

public sealed class AiToolExecutionRepository : IAiToolExecutionRepository
{
    private readonly AiDbContext _db;

    public AiToolExecutionRepository(AiDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AiToolExecution execution, CancellationToken ct = default)
        => await _db.AiToolExecutions.AddAsync(execution, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
