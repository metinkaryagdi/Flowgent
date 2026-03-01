using AutoMapper;
using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.MarkNotificationRead;
using BitirmeProject.NotificationService.Domain.Entities;
using BitirmeProject.NotificationService.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace NotificationService.UnitTests.Application.Handlers;

public sealed class MarkNotificationReadCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenNotificationMissing()
    {
        var repository = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Notification?)null);

        var handler = new MarkNotificationReadCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenUserMismatch()
    {
        var repository = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var notification = new Notification(Guid.NewGuid(), "Title", "Body", NotificationChannel.InApp, "Issue", Guid.NewGuid(), null);
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(notification);

        var handler = new MarkNotificationReadCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new MarkNotificationReadCommand(notification.Id, Guid.NewGuid());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MarksRead_AndWritesOutbox()
    {
        var repository = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var userId = Guid.NewGuid();
        var notification = new Notification(userId, "Title", "Body", NotificationChannel.InApp, "Issue", Guid.NewGuid(), null);
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(notification);

        var expected = new NotificationDto { Id = notification.Id };
        mapper.Map<NotificationDto>(Arg.Any<Notification>()).Returns(expected);

        var handler = new MarkNotificationReadCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new MarkNotificationReadCommand(notification.Id, userId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expected);
        await repository.Received(1).UpdateAsync(notification, Arg.Any<CancellationToken>());
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m => m.EventType == "NotificationReadEvent"), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<NotificationDto>(notification);
    }
}
