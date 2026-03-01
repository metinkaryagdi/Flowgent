using BitirmeProject.NotificationService.Api.Events.Handlers;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Contracts.Events;

namespace NotificationService.UnitTests.Consumers;

public sealed class MemberAddedEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_SendsCreateNotificationCommand()
    {
        var logger = Substitute.For<ILogger<MemberAddedEventHandler>>();
        var mediator = Substitute.For<IMediator>();

        var handler = new MemberAddedEventHandler(logger, mediator);
        var evt = new MemberAddedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.Received(1).Send(Arg.Is<CreateNotificationCommand>(c =>
            c.UserId == evt.UserId &&
            c.EntityType == "Project" &&
            c.EntityId == evt.ProjectId), Arg.Any<CancellationToken>());
    }
}
