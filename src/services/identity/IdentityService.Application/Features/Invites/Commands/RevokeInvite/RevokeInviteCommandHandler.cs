using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Commands.RevokeInvite;

public sealed class RevokeInviteCommandHandler : IRequestHandler<RevokeInviteCommand>
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeInviteCommandHandler(
        IInviteRepository inviteRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _inviteRepository = inviteRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RevokeInviteCommand request, CancellationToken cancellationToken)
    {
        var invite = await _inviteRepository.GetByIdAsync(request.InviteId, cancellationToken)
            ?? throw new InvalidOperationException("Invite not found.");

        var organization = await _organizationRepository.GetByIdAsync(invite.OrganizationId, cancellationToken)
            ?? throw new InvalidOperationException("Organization not found.");

        var requesterRole = organization.GetMemberRole(request.RequestedByUserId)
            ?? throw new UnauthorizedAccessException("You are not a member of this organization.");

        if (requesterRole == OrganizationRole.Member)
            throw new UnauthorizedAccessException("Only Owner or Manager can revoke invites.");

        invite.Revoke();
        await _inviteRepository.UpdateAsync(invite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
