using BitirmeProject.NotificationService.Api.Events.Handlers;
using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Contracts.Events;

namespace NotificationService.UnitTests.Consumers;

public sealed class CommentAddedEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_Ignores_WhenRecipientIsAuthor()
    {
        var logger = Substitute.For<ILogger<CommentAddedEventHandler>>();
        var mediator = Substitute.For<IMediator>();

        var authorId = Guid.NewGuid();
        var handler = new CommentAddedEventHandler(logger, mediator);
        // AssigneeUserId == authorId => skip
        var evt = new CommentAddedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), authorId, "Test Issue", Guid.NewGuid(), authorId, Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.DidNotReceive().Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Ignores_WhenNoRecipient()
    {
        var logger = Substitute.For<ILogger<CommentAddedEventHandler>>();
        var mediator = Substitute.For<IMediator>();

        var authorId = Guid.NewGuid();
        var handler = new CommentAddedEventHandler(logger, mediator);
        // CreatedByUserId == authorId, no assignee => skip
        var evt = new CommentAddedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), authorId, "Test Issue", authorId, null, Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.DidNotReceive().Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SendsNotification_ToAssignee_WhenDifferentFromAuthor()
    {
        var logger = Substitute.For<ILogger<CommentAddedEventHandler>>();
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationDto { Id = Guid.NewGuid() });

        var authorId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var issueId = Guid.NewGuid();
        var handler = new CommentAddedEventHandler(logger, mediator);
        var evt = new CommentAddedEvent(Guid.NewGuid(), issueId, Guid.NewGuid(), authorId, "Test Issue", Guid.NewGuid(), assigneeId, Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.Received(1).Send(Arg.Is<CreateNotificationCommand>(c =>
            c.UserId == assigneeId &&
            c.EntityType == "Issue" &&
            c.EntityId == issueId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SendsNotification_ToCreatedBy_WhenNoAssignee()
    {
        var logger = Substitute.For<ILogger<CommentAddedEventHandler>>();
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationDto { Id = Guid.NewGuid() });

        var authorId = Guid.NewGuid();
        var createdById = Guid.NewGuid();
        var issueId = Guid.NewGuid();
        var handler = new CommentAddedEventHandler(logger, mediator);
        var evt = new CommentAddedEvent(Guid.NewGuid(), issueId, Guid.NewGuid(), authorId, "Test Issue", createdById, null, Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.Received(1).Send(Arg.Is<CreateNotificationCommand>(c =>
            c.UserId == createdById &&
            c.EntityType == "Issue" &&
            c.EntityId == issueId), Arg.Any<CancellationToken>());
    }
}
