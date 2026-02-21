using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BitirmeProject.NotificationService.Api.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst("userId")?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnConnectedAsync();
    }
}
