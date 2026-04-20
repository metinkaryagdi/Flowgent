using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.NotificationService.Api.Events.Handlers;

public sealed class UserInvitedEventHandler : IEventHandler<UserInvitedEvent>
{
    private readonly ILogger<UserInvitedEventHandler> _logger;
    private readonly IMediator _mediator;

    public UserInvitedEventHandler(ILogger<UserInvitedEventHandler> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task HandleAsync(UserInvitedEvent @event, CancellationToken cancellationToken = default)
    {
        // Only create an in-app notification if the invited user already has an account
        if (@event.InvitedUserId is null)
        {
            _logger.LogInformation(
                "UserInvitedEvent: {Email} has no account yet, skipping in-app notification.",
                @event.Email);
            return;
        }

        var command = new CreateNotificationCommand(
            @event.InvitedUserId.Value,
            "Organizasyon daveti",
            $"{@event.OrganizationName} organizasyonuna {@event.Role} olarak davet edildiniz.",
            "InApp",
            "Organization",
            @event.OrganizationId,
            @event.CorrelationId,
            @event.EventId);

        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "UserInvitedEvent handled. OrgId={OrgId} InvitedUserId={UserId}",
            @event.OrganizationId, @event.InvitedUserId);
    }
}
