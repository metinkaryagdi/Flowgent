using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.NotificationService.Api.Events.Handlers;

public sealed class IssueStatusChangedEventHandler : IEventHandler<IssueStatusChangedEvent>
{
    private readonly ILogger<IssueStatusChangedEventHandler> _logger;
    private readonly IMediator _mediator;

    public IssueStatusChangedEventHandler(
        ILogger<IssueStatusChangedEventHandler> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task HandleAsync(IssueStatusChangedEvent @event, CancellationToken cancellationToken = default)
    {
        var recipient = ResolveRecipient(@event);
        if (recipient == Guid.Empty)
            return;

        var command = new CreateNotificationCommand(
            recipient,
            "Issue status changed",
            $"Issue \"{@event.IssueTitle}\" status changed from {@event.OldStatus} to {@event.NewStatus}.",
            "InApp",
            "Issue",
            @event.IssueId,
            @event.CorrelationId,
            @event.EventId);

        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "IssueStatusChangedEvent handled. IssueId={IssueId}, RecipientUserId={RecipientUserId}",
            @event.IssueId,
            recipient);
    }

    private static Guid ResolveRecipient(IssueStatusChangedEvent @event)
    {
        if (@event.AssigneeUserId.HasValue && @event.AssigneeUserId.Value != @event.ChangedByUserId)
            return @event.AssigneeUserId.Value;

        if (@event.CreatedByUserId != @event.ChangedByUserId)
            return @event.CreatedByUserId;

        return Guid.Empty;
    }
}
