using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Queries.GetPendingInvites;

public sealed class GetPendingInvitesQueryHandler
    : IRequestHandler<GetPendingInvitesQuery, IReadOnlyList<InviteDto>>
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public GetPendingInvitesQueryHandler(
        IInviteRepository inviteRepository,
        IOrganizationRepository organizationRepository)
    {
        _inviteRepository = inviteRepository;
        _organizationRepository = organizationRepository;
    }

    public async Task<IReadOnlyList<InviteDto>> Handle(
        GetPendingInvitesQuery request,
        CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken)
            ?? throw new InvalidOperationException("Organization not found.");

        var requesterRole = organization.GetMemberRole(request.RequestedByUserId)
            ?? throw new UnauthorizedAccessException("You are not a member of this organization.");

        if (requesterRole == OrganizationRole.Member)
            throw new UnauthorizedAccessException("Only Owner or Manager can view pending invites.");

        var invites = await _inviteRepository.GetPendingByOrganizationAsync(
            request.OrganizationId, cancellationToken);

        return invites.Select(i => new InviteDto
        {
            Id = i.Id,
            Email = i.Email,
            Role = i.Role.ToString(),
            ExpiresAt = i.ExpiresAt,
            CreatedAt = i.CreatedAt
        }).ToList();
    }
}
