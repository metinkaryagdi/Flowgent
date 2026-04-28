using BitirmeProject.AiService.Domain.Entities;

namespace BitirmeProject.AiService.Application.Abstractions;

public interface IAiToolExecutionRepository
{
    Task AddAsync(AiToolExecution execution, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
