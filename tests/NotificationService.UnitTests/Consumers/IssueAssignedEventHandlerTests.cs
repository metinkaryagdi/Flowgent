using BitirmeProject.NotificationService.Api.Events.Handlers;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Contracts.Events;

namespace NotificationService.UnitTests.Consumers;

public sealed class IssueAssignedEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_SendsCreateNotificationCommand()
    {
        var logger = Substitute.For<ILogger<IssueAssignedEventHandler>>();
        var mediator = Substitute.For<IMediator>();

        var handler = new IssueAssignedEventHandler(logger, mediator);
        var evt = new IssueAssignedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.Received(1).Send(Arg.Is<CreateNotificationCommand>(c =>
            c.UserId == evt.AssigneeUserId &&
            c.EntityType == "Issue" &&
            c.EntityId == evt.IssueId), Arg.Any<CancellationToken>());
    }
}
