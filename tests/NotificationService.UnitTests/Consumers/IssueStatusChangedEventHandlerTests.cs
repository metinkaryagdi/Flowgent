using BitirmeProject.NotificationService.Api.Events.Handlers;
using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Contracts.Events;

namespace NotificationService.UnitTests.Consumers;

public sealed class IssueStatusChangedEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_Ignores_WhenActorIsAssigneeAndCreator()
    {
        var logger = Substitute.For<ILogger<IssueStatusChangedEventHandler>>();
        var mediator = Substitute.For<IMediator>();

        var actorId = Guid.NewGuid();
        var handler = new IssueStatusChangedEventHandler(logger, mediator);
        var evt = new IssueStatusChangedEvent(Guid.NewGuid(), Guid.NewGuid(), "Open", "Done", actorId, "Test Issue", actorId, actorId, Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.DidNotReceive().Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SendsNotification_ToAssignee_WhenActorDifferent()
    {
        var logger = Substitute.For<ILogger<IssueStatusChangedEventHandler>>();
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationDto { Id = Guid.NewGuid() });

        var actorId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var issueId = Guid.NewGuid();
        var handler = new IssueStatusChangedEventHandler(logger, mediator);
        var evt = new IssueStatusChangedEvent(issueId, Guid.NewGuid(), "Open", "Done", actorId, "Test Issue", Guid.NewGuid(), assigneeId, Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.Received(1).Send(Arg.Is<CreateNotificationCommand>(c =>
            c.UserId == assigneeId &&
            c.EntityType == "Issue" &&
            c.EntityId == issueId &&
            c.ExternalEventId == evt.EventId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SendsNotification_ToCreatedBy_WhenNoAssigneeAndActorDifferent()
    {
        var logger = Substitute.For<ILogger<IssueStatusChangedEventHandler>>();
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationDto { Id = Guid.NewGuid() });

        var actorId = Guid.NewGuid();
        var createdById = Guid.NewGuid();
        var issueId = Guid.NewGuid();
        var handler = new IssueStatusChangedEventHandler(logger, mediator);
        var evt = new IssueStatusChangedEvent(issueId, Guid.NewGuid(), "Open", "Done", actorId, "Test Issue", createdById, null, Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.Received(1).Send(Arg.Is<CreateNotificationCommand>(c =>
            c.UserId == createdById &&
            c.EntityType == "Issue" &&
            c.EntityId == issueId), Arg.Any<CancellationToken>());
    }
}
