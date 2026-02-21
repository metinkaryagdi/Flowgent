using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;
using BitirmeProject.NotificationService.Api.Hubs;
using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using MediatR;

namespace BitirmeProject.NotificationService.Api.Events.Handlers;

public sealed class NotificationRequestedEventHandler : IEventHandler<NotificationRequestedEvent>
{
    private readonly ILogger<NotificationRequestedEventHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly IEmailSender _emailSender;

    public NotificationRequestedEventHandler(
        ILogger<NotificationRequestedEventHandler> logger,
        IMediator mediator,
        IHubContext<NotificationsHub> hubContext,
        IEmailSender emailSender)
    {
        _logger = logger;
        _mediator = mediator;
        _hubContext = hubContext;
        _emailSender = emailSender;
    }

    public async Task HandleAsync(NotificationRequestedEvent @event, CancellationToken cancellationToken = default)
    {
        var command = new CreateNotificationCommand(
            @event.UserId,
            @event.Title,
            @event.Message,
            @event.Channel,
            @event.EntityType,
            @event.EntityId,
            @event.CorrelationId,
            @event.EventId);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.Channel == Domain.Enums.NotificationChannel.InApp)
        {
            await _hubContext.Clients
                .Group($"user-{@event.UserId}")
                .SendAsync("notification", result, cancellationToken);
        }
        else if (result.Channel == Domain.Enums.NotificationChannel.Email)
        {
            await _emailSender.SendAsync(@event.UserId, @event.Title, @event.Message, cancellationToken);
        }

        _logger.LogInformation(
            "NotificationRequestedEvent handled. NotificationId={NotificationId}, UserId={UserId}",
            result.Id,
            @event.UserId);
    }
}
