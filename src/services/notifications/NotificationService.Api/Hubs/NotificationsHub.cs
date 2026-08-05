using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shared.Common.Extensions;

namespace BitirmeProject.NotificationService.Api.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub
{
    /// <summary>Group a connection joins so the delivery worker can target one user.</summary>
    public static string GroupFor(Guid userId) => $"user-{userId}";

    /// <summary>
    /// Client method invoked when a new notification is delivered. Shared as a constant
    /// so the server and the web client cannot drift apart -- they already had, and the
    /// mismatch was invisible because sending to a group never fails.
    /// </summary>
    public const string ReceiveNotification = "ReceiveNotification";

    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(ILogger<NotificationsHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        // Must go through the shared helper: JwtBearer runs with MapInboundClaims
        // enabled, so the raw "sub" claim has already been renamed to
        // ClaimTypes.NameIdentifier by the time it reaches here. Reading "sub"
        // directly returned null and left every connection outside its group, which
        // silently dropped all real-time notifications.
        var userId = Context.User.TryGetUserId();

        if (userId is null)
        {
            _logger.LogWarning(
                "Hub connection {ConnectionId} has no usable user id claim; it will receive no notifications.",
                Context.ConnectionId);
        }
        else
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(userId.Value));
            _logger.LogInformation(
                "Hub connection {ConnectionId} joined {Group}.",
                Context.ConnectionId, GroupFor(userId.Value));
        }

        await base.OnConnectedAsync();
    }
}
