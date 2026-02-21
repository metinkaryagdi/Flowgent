using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.NotificationService.Api.Events.Handlers;

public sealed class IssueAssignedEventHandler : IEventHandler<IssueAssignedEvent>
{
    private readonly ILogger<IssueAssignedEventHandler> _logger;
    private readonly IMediator _mediator;

    public IssueAssignedEventHandler(ILogger<IssueAssignedEventHandler> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task HandleAsync(IssueAssignedEvent @event, CancellationToken cancellationToken = default)
    {
        var command = new CreateNotificationCommand(
            @event.AssigneeUserId,
            "Issue assigned",
            $"You were assigned to issue {@event.IssueId}.",
            "InApp",
            "Issue",
            @event.IssueId,
            @event.CorrelationId,
            @event.EventId);

        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("IssueAssignedEvent handled. IssueId={IssueId}", @event.IssueId);
    }
}
