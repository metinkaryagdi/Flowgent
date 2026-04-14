using BitirmeProject.IdentityService.Domain.Entities;

namespace BitirmeProject.IdentityService.Application.Abstractions;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Organization?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Organization>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Organization>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);
    Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default);
}
