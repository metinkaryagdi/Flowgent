using AutoMapper;
using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using BitirmeProject.NotificationService.Domain.Entities;
using BitirmeProject.NotificationService.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace NotificationService.UnitTests.Application.Handlers;

public sealed class CreateNotificationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsExisting_WhenExternalEventIdMatches()
    {
        var repository = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var existing = new Notification(Guid.NewGuid(), "Title", "Body", NotificationChannel.InApp, "Issue", Guid.NewGuid(), Guid.NewGuid());
        repository.GetByExternalEventIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(existing);

        var expected = new NotificationDto { Id = existing.Id };
        mapper.Map<NotificationDto>(existing).Returns(expected);

        var handler = new CreateNotificationCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new CreateNotificationCommand(existing.UserId, "Title", "Body", "InApp", "Issue", existing.EntityId, null, existing.ExternalEventId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expected);
        await repository.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenChannelUnsupported()
    {
        var repository = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var handler = new CreateNotificationCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new CreateNotificationCommand(Guid.NewGuid(), "Title", "Body", "Sms", "Issue", Guid.NewGuid(), null, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await repository.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreatesNotification_AndWritesOutbox()
    {
        var repository = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.GetByExternalEventIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Notification?)null);

        Notification? captured = null;
        repository.AddAsync(Arg.Do<Notification>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var expected = new NotificationDto { Id = Guid.NewGuid() };
        mapper.Map<NotificationDto>(Arg.Any<Notification>()).Returns(expected);

        var handler = new CreateNotificationCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new CreateNotificationCommand(Guid.NewGuid(), "Title", "Body", "InApp", "Issue", Guid.NewGuid(), null, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expected);
        captured.Should().NotBeNull();
        captured!.Channel.Should().Be(NotificationChannel.InApp);
        captured.Status.Should().Be(NotificationStatus.Delivered);
        captured.IsRead.Should().BeFalse();

        await repository.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m =>
            m.EventType == "NotificationCreatedEvent" &&
            !string.IsNullOrWhiteSpace(m.Payload)), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<NotificationDto>(Arg.Any<Notification>());
    }
}
