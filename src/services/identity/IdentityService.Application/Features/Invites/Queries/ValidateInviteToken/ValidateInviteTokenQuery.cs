using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Queries.ValidateInviteToken;

public sealed record ValidateInviteTokenQuery(Guid Token) : IRequest<ValidateInviteTokenResult?>;

public sealed class ValidateInviteTokenResult
{
    public string Email { get; init; } = null!;
    public string OrganizationName { get; init; } = null!;
    public string Role { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
}
