using System.Security.Claims;
using BitirmeProject.NotificationService.Api.Controllers;
using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.MarkNotificationRead;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace NotificationService.UnitTests.Controllers;

public sealed class NotificationsControllerTests
{
    [Fact]
    public async Task Create_ReturnsOk_WithResult()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new NotificationsController(mediator);
        var command = new CreateNotificationCommand(Guid.NewGuid(), "Title", "Body", "InApp", "Issue", Guid.NewGuid(), null);
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

        var result = await controller.MarkRead(Guid.NewGuid());

        result.Result.Should().BeOfType<UnauthorizedResult>();
        await mediator.DidNotReceive().Send(Arg.Any<MarkNotificationReadCommand>());
    }

    [Fact]
    public async Task MarkRead_UsesUserIdFromClaims()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new NotificationsController(mediator);
        var userId = Guid.NewGuid();

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Test");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };

        var dto = new NotificationDto { Id = Guid.NewGuid() };
        mediator.Send(Arg.Any<MarkNotificationReadCommand>()).Returns(dto);

        var result = await controller.MarkRead(Guid.NewGuid());

        result.Result.Should().BeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Is<MarkNotificationReadCommand>(c => c.UserId == userId));
    }
}
