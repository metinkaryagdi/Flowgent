using AutoMapper;
using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Application.Features.Notifications.Queries.GetByUser;
using BitirmeProject.NotificationService.Domain.Entities;
using BitirmeProject.NotificationService.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace NotificationService.UnitTests.Application.Handlers;

public sealed class GetNotificationsByUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedList()
    {
        var repository = Substitute.For<INotificationRepository>();
        var mapper = Substitute.For<IMapper>();

        var items = new List<Notification>
        {
            new Notification(Guid.NewGuid(), "T1", "Body", NotificationChannel.InApp, "Issue", Guid.NewGuid(), null),
            new Notification(Guid.NewGuid(), "T2", "Body", NotificationChannel.InApp, "Issue", Guid.NewGuid(), null)
        };
        repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(items);

        var dto1 = new NotificationDto { Id = items[0].Id };
        var dto2 = new NotificationDto { Id = items[1].Id };
        mapper.Map<NotificationDto>(items[0]).Returns(dto1);
        mapper.Map<NotificationDto>(items[1]).Returns(dto2);

        var handler = new GetNotificationsByUserQueryHandler(repository, mapper);
        var query = new GetNotificationsByUserQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainInOrder(dto1, dto2);
    }
}
