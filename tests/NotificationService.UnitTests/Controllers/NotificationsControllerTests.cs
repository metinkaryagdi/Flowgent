using System.Security.Claims;
using BitirmeProject.NotificationService.Api.Controllers;
using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.MarkNotificationRead;
using BitirmeProject.NotificationService.Application.Features.Notifications.Queries.GetByUser;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace NotificationService.UnitTests.Controllers;

public sealed class NotificationsControllerTests
{
    private static ControllerContext MakeContext(Guid? userId = null, bool isAdmin = false)
    {
        var claims = new List<Claim>();
        if (userId.HasValue)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        if (isAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };
    }

    [Fact]
    public async Task Create_ReturnsOk_WithResult()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new NotificationsController(mediator);
        var command = new CreateNotificationCommand(Guid.NewGuid(), "Title", "Body", "InApp", "Issue", Guid.NewGuid(), null, null);
        var dto = new NotificationDto { Id = Guid.NewGuid() };
        mediator.Send(command).Returns(dto);

        var result = await controller.Create(command);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task MarkRead_ReturnsUnauthorized_WhenNoUser()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new NotificationsController(mediator)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.MarkRead(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        await mediator.DidNotReceive().Send(Arg.Any<MarkNotificationReadCommand>());
    }

    [Fact]
    public async Task MarkRead_UsesUserIdFromClaims()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        var controller = new NotificationsController(mediator)
        {
            ControllerContext = MakeContext(userId)
        };

        var dto = new NotificationDto { Id = Guid.NewGuid() };
        mediator.Send(Arg.Any<MarkNotificationReadCommand>()).Returns(dto);

        var result = await controller.MarkRead(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Is<MarkNotificationReadCommand>(c => c.UserId == userId));
    }

    [Fact]
    public async Task GetByUser_ReturnsForbid_WhenAccessingOtherUsersNotifications()
    {
        var mediator = Substitute.For<IMediator>();
        var requesterId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var controller = new NotificationsController(mediator)
        {
            ControllerContext = MakeContext(requesterId)
        };

        var result = await controller.GetByUser(otherUserId);

        result.Result.Should().BeOfType<ForbidResult>();
        await mediator.DidNotReceive().Send(Arg.Any<GetNotificationsByUserQuery>());
    }

    [Fact]
    public async Task GetByUser_ReturnsOk_WhenAccessingOwnNotifications()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        var controller = new NotificationsController(mediator)
        {
            ControllerContext = MakeContext(userId)
        };
        mediator.Send(Arg.Any<GetNotificationsByUserQuery>()).Returns(new List<NotificationDto>());

        var result = await controller.GetByUser(userId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByUser_ReturnsOk_WhenAdmin_AccessesAnyUser()
    {
        var mediator = Substitute.For<IMediator>();
        var adminId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var controller = new NotificationsController(mediator)
        {
            ControllerContext = MakeContext(adminId, isAdmin: true)
        };
        mediator.Send(Arg.Any<GetNotificationsByUserQuery>()).Returns(new List<NotificationDto>());

        var result = await controller.GetByUser(otherUserId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
