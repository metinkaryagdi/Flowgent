using BitirmeProject.IdentityService.Application.Abstractions;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Queries.ValidateInviteToken;

public sealed class ValidateInviteTokenQueryHandler
    : IRequestHandler<ValidateInviteTokenQuery, ValidateInviteTokenResult?>
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public ValidateInviteTokenQueryHandler(
        IInviteRepository inviteRepository,
        IOrganizationRepository organizationRepository)
    {
        _inviteRepository = inviteRepository;
        _organizationRepository = organizationRepository;
    }

    public async Task<ValidateInviteTokenResult?> Handle(
        ValidateInviteTokenQuery request,
        CancellationToken cancellationToken)
    {
        var invite = await _inviteRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (invite is null || !invite.IsValid)
            return null;

        var organization = await _organizationRepository.GetByIdAsync(invite.OrganizationId, cancellationToken);
        if (organization is null)
            return null;

        return new ValidateInviteTokenResult
        {
            Email = invite.Email,
            OrganizationName = organization.Name,
            Role = invite.Role.ToString(),
            ExpiresAt = invite.ExpiresAt
        };
    }
}
