using System.Text.Json;
using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Entities;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Commands.SendInvite;

public sealed class SendInviteCommandHandler : IRequestHandler<SendInviteCommand, InviteDto>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IInviteRepository _inviteRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SendInviteCommandHandler> _logger;

    public SendInviteCommandHandler(
        IOrganizationRepository organizationRepository,
        IInviteRepository inviteRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper,
        ILogger<SendInviteCommandHandler> logger)
    {
        _organizationRepository = organizationRepository;
        _inviteRepository = inviteRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InviteDto> Handle(SendInviteCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAndUserIdAsync(request.OrganizationId, request.InvitedByUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Organization not found or you are not a member.");

        var requesterRole = organization.Members.FirstOrDefault(m => m.UserId == request.InvitedByUserId)?.Role
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

        // Look up the invited user's ID if they already have an account
        var invitedUser = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);
        var invitedUserId = invitedUser?.Id;

        var evt = new UserInvitedEvent(
            request.Email,
            organization.Id,
            organization.Name,
            request.InvitedByUserId,
            request.Role.ToString(),
            invitedUserId);

        await _outboxRepository.AddAsync(new OutboxMessage
        {
            EventType = nameof(UserInvitedEvent),
            Payload = JsonSerializer.Serialize(evt),
            OccurredOn = evt.OccurredOn
        }, cancellationToken);

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
