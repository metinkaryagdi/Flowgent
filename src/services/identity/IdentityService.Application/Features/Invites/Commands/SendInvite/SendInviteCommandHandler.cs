using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Entities;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Commands.SendInvite;

public sealed class SendInviteCommandHandler : IRequestHandler<SendInviteCommand, InviteDto>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IInviteRepository _inviteRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SendInviteCommandHandler> _logger;

    public SendInviteCommandHandler(
        IOrganizationRepository organizationRepository,
        IInviteRepository inviteRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SendInviteCommandHandler> logger)
    {
        _organizationRepository = organizationRepository;
        _inviteRepository = inviteRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
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
        try
        {
            await _emailService.SendInviteEmailAsync(request.Email, organization.Name, inviteLink, cancellationToken);
        }
        catch (Exception ex)
        {
            // Email delivery is best-effort. The invite token is already saved;
            // the inviter can share the link manually if needed.
            _logger.LogWarning(ex,
                "Invite email could not be delivered to {Email} for org {OrgId}. Invite token: {Token}",
                request.Email, request.OrganizationId, invite.Token);
        }

        return _mapper.Map<InviteDto>(invite);
    }
}
