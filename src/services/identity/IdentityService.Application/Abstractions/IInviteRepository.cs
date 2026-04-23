using BitirmeProject.IdentityService.Domain.Entities;

namespace BitirmeProject.IdentityService.Application.Abstractions;

public interface IInviteRepository
{
    Task<InviteToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InviteToken?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
    Task<InviteToken?> GetPendingByEmailAndOrganizationAsync(string email, Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InviteToken>> GetPendingByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingInviteAsync(string email, Guid organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(InviteToken invite, CancellationToken cancellationToken = default);
    Task UpdateAsync(InviteToken invite, CancellationToken cancellationToken = default);
}
