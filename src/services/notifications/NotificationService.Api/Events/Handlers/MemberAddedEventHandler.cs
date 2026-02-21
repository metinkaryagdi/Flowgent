using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.NotificationService.Api.Events.Handlers;

public sealed class MemberAddedEventHandler : IEventHandler<MemberAddedEvent>
{
    private readonly ILogger<MemberAddedEventHandler> _logger;
    private readonly IMediator _mediator;

    public MemberAddedEventHandler(ILogger<MemberAddedEventHandler> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task HandleAsync(MemberAddedEvent @event, CancellationToken cancellationToken = default)
    {
        var command = new CreateNotificationCommand(
            @event.UserId,
            "Project access granted",
            $"You were added to project {@event.ProjectId}.",
            "InApp",
            "Project",
            @event.ProjectId,
            @event.CorrelationId,
            @event.EventId);

        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("MemberAddedEvent handled. ProjectId={ProjectId}", @event.ProjectId);
    }
}
