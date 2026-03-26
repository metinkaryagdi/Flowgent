using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetActiveSprint;
using BitirmeProject.SprintService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace SprintService.UnitTests.Application.Handlers;

public sealed class GetActiveSprintQueryHandlerTests
{
    private static Sprint CreateSprint()
    {
        var startDate = DateTime.UtcNow.Date;
        return new Sprint(Guid.NewGuid(), "Sprint", null, startDate, startDate.AddDays(14), Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenNoActiveSprint()
    {
        var repository = Substitute.For<ISprintRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.GetActiveByProjectIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sprint?)null);

        var handler = new GetActiveSprintQueryHandler(repository, mapper);
        var query = new GetActiveSprintQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenActiveSprintExists()
    {
        var repository = Substitute.For<ISprintRepository>();
        var mapper = Substitute.For<IMapper>();

        var sprint = CreateSprint();
        repository.GetActiveByProjectIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprint);

        var expected = new SprintDto { Id = sprint.Id };
        mapper.Map<SprintDto>(sprint).Returns(expected);

        var handler = new GetActiveSprintQueryHandler(repository, mapper);
        var query = new GetActiveSprintQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().Be(expected);
    }
}
