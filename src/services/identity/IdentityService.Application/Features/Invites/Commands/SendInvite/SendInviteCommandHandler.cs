using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Entities;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Commands.SendInvite;

public sealed class SendInviteCommandHandler : IRequestHandler<SendInviteCommand, InviteDto>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IInviteRepository _inviteRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public SendInviteCommandHandler(
        IOrganizationRepository organizationRepository,
        IInviteRepository inviteRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _inviteRepository = inviteRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<InviteDto> Handle(SendInviteCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken)
            ?? throw new InvalidOperationException("Organization not found.");

        var requesterRole = organization.GetMemberRole(request.InvitedByUserId)
            ?? throw new UnauthorizedAccessException("You are not a member of this organization.");

        if (requesterRole == OrganizationRole.Member)
            throw new UnauthorizedAccessException("Only Owner or Manager can send invites.");

        var hasPending = await _inviteRepository.HasPendingInviteAsync(
            request.Email, request.OrganizationId, cancellationToken);

        if (hasPending)
            throw new InvalidOperationException("A pending invite already exists for this email.");

        var invite = new InviteToken(
            request.Email,
            request.OrganizationId,
            request.InvitedByUserId,
            request.Role);

        await _inviteRepository.AddAsync(invite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var inviteLink = $"{request.InviteLinkBaseUrl}/invite/accept?token={invite.Token}";
        await _emailService.SendInviteEmailAsync(request.Email, organization.Name, inviteLink, cancellationToken);

        return new InviteDto
        {
            Id = invite.Id,
            Email = invite.Email,
            Role = invite.Role.ToString(),
            ExpiresAt = invite.ExpiresAt,
            CreatedAt = invite.CreatedAt
        };
    }
}
