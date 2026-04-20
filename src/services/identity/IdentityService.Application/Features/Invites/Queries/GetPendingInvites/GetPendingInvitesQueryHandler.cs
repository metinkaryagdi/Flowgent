using AutoMapper;
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
    private readonly IMapper _mapper;

    public GetPendingInvitesQueryHandler(
        IInviteRepository inviteRepository,
        IOrganizationRepository organizationRepository,
        IMapper mapper)
    {
        _inviteRepository = inviteRepository;
        _organizationRepository = organizationRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<InviteDto>> Handle(
        GetPendingInvitesQuery request,
        CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAndUserIdAsync(request.OrganizationId, request.RequestedByUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Organization not found or you are not a member.");

        var requesterRole = organization.Members.FirstOrDefault(m => m.UserId == request.RequestedByUserId)?.Role
            ?? throw new UnauthorizedAccessException("You are not a member of this organization.");

        if (requesterRole == OrganizationRole.Member)
            throw new UnauthorizedAccessException("Only Owner or Manager can view pending invites.");

        var invites = await _inviteRepository.GetPendingByOrganizationAsync(
            request.OrganizationId, cancellationToken);

        return _mapper.Map<List<InviteDto>>(invites);
    }
}
