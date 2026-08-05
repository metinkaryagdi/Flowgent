using BitirmeProject.NotificationService.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.NotificationService.Api.Events.Handlers;

/// <summary>
/// Pushes "this notification is now read" to the user's other open tabs.
///
/// MarkNotificationReadCommandHandler has been writing NotificationReadEvent to the outbox
/// all along, but nothing consumed it -- so the web client, which has always subscribed to
/// the NotificationRead hub method, never heard anything and marking a notification read in
/// one tab left the others showing it as unread.
///
/// Going back out through the broker rather than pushing straight from the command handler
/// keeps SignalR out of the Application layer and is what makes this correct with more than
/// one replica: whichever replica consumes the message publishes to the group, and the Redis
/// backplane fans it out to the replica that actually holds the user's connection.
/// </summary>
public sealed class NotificationReadEventHandler : IEventHandler<NotificationReadEvent>
{
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly ILogger<NotificationReadEventHandler> _logger;

    public NotificationReadEventHandler(
        IHubContext<NotificationsHub> hubContext,
        ILogger<NotificationReadEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleAsync(NotificationReadEvent @event, CancellationToken cancellationToken = default)
    {
        // Sending to a group nobody has joined is not an error, so this call succeeding
        // says nothing about anyone having received it. The E2E test is what proves the
        // client is actually listening on this method name.
        await _hubContext.Clients
            .Group(NotificationsHub.GroupFor(@event.UserId))
            .SendAsync(
                NotificationsHub.NotificationRead,
                new { notificationId = @event.NotificationId, readAt = @event.ReadAtUtc },
                cancellationToken);

        _logger.LogInformation(
            "NotificationReadEvent pushed. NotificationId={NotificationId}, UserId={UserId}",
            @event.NotificationId,
            @event.UserId);
    }
}
