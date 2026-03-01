using BitirmeProject.NotificationService.Api.Events.Handlers;
using BitirmeProject.NotificationService.Api.Hubs;
using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Contracts.Events;

namespace NotificationService.UnitTests.Consumers;

public sealed class NotificationRequestedEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_SendsHubMessage_WhenChannelInApp()
    {
        var logger = Substitute.For<ILogger<NotificationRequestedEventHandler>>();
        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<NotificationsHub>>();
        var emailSender = Substitute.For<IEmailSender>();

        var clients = Substitute.For<IHubClients>();
        var proxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Returns(clients);
        clients.Group(Arg.Any<string>()).Returns(proxy);

        mediator.Send(Arg.Any<BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification.CreateNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationDto { Id = Guid.NewGuid(), Channel = NotificationChannel.InApp });

        var handler = new NotificationRequestedEventHandler(logger, mediator, hubContext, emailSender);
        var evt = new NotificationRequestedEvent(Guid.NewGuid(), Guid.NewGuid(), "Title", "Message", "InApp", "Issue", Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await proxy.Received(1).SendAsync("notification", Arg.Any<object?>(), Arg.Any<CancellationToken>());
        await emailSender.DidNotReceive().SendAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SendsEmail_WhenChannelEmail()
    {
        var logger = Substitute.For<ILogger<NotificationRequestedEventHandler>>();
        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<NotificationsHub>>();
        var emailSender = Substitute.For<IEmailSender>();

        mediator.Send(Arg.Any<BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification.CreateNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationDto { Id = Guid.NewGuid(), Channel = NotificationChannel.Email });

        var handler = new NotificationRequestedEventHandler(logger, mediator, hubContext, emailSender);
        var evt = new NotificationRequestedEvent(Guid.NewGuid(), Guid.NewGuid(), "Title", "Message", "Email", "Issue", Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await emailSender.Received(1).SendAsync(evt.UserId, evt.Title, evt.Message, Arg.Any<CancellationToken>());
    }
}
