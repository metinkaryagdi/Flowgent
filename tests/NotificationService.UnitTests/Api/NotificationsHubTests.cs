using System.Security.Claims;
using BitirmeProject.NotificationService.Api.Hubs;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace NotificationService.UnitTests.Api;

/// <summary>
/// Guards the hub's group membership.
///
/// This shipped broken: the hub read the raw "sub" claim, but JwtBearer runs with
/// MapInboundClaims enabled, which renames it to ClaimTypes.NameIdentifier before the
/// hub sees it. Every connection therefore stayed outside its group and no real-time
/// notification was ever delivered -- silently, because sending to a group with no
/// members is not an error and the notification was still marked "delivered".
/// </summary>
public sealed class NotificationsHubTests
{
    private static (NotificationsHub Hub, IGroupManager Groups) CreateHub(ClaimsPrincipal? user, string connectionId = "conn-1")
    {
        var groups = Substitute.For<IGroupManager>();
        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns(connectionId);
        context.User.Returns(user);

        var hub = new NotificationsHub(NullLogger<NotificationsHub>.Instance)
        {
            Context = context,
            Groups = groups
        };

        return (hub, groups);
    }

    [Fact]
    public async Task OnConnectedAsync_JoinsUserGroup_WhenIdentityUsesNameIdentifier()
    {
        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"));

        var (hub, groups) = CreateHub(user);

        await hub.OnConnectedAsync();

        await groups.Received(1).AddToGroupAsync(
            "conn-1", NotificationsHub.GroupFor(userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnConnectedAsync_JoinsUserGroup_WhenIdentityUsesSubClaim()
    {
        // Tokens that reach the hub unmapped still have to work.
        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", userId.ToString())], "Test"));

        var (hub, groups) = CreateHub(user);

        await hub.OnConnectedAsync();

        await groups.Received(1).AddToGroupAsync(
            "conn-1", NotificationsHub.GroupFor(userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnConnectedAsync_DoesNotJoinAnyGroup_WhenUserIdIsMissing()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("email", "x@y.z")], "Test"));

        var (hub, groups) = CreateHub(user);

        await hub.OnConnectedAsync();

        await groups.DidNotReceive().AddToGroupAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GroupFor_MatchesTheNameTheDeliveryWorkerTargets()
    {
        var userId = Guid.NewGuid();

        NotificationsHub.GroupFor(userId).Should().Be($"user-{userId}");
    }

    [Fact]
    public void ReceiveNotification_MatchesTheWebClientHandlerName()
    {
        // src/frontend/web/src/hooks/useSignalR.ts subscribes with this exact name.
        // A rename on either side delivers to nobody without raising an error.
        NotificationsHub.ReceiveNotification.Should().Be("ReceiveNotification");
    }
}
