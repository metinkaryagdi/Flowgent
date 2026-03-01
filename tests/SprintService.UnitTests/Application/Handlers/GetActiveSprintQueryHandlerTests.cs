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

        var sprint = new Sprint(Guid.NewGuid(), "Sprint", null, Guid.NewGuid());
        repository.GetActiveByProjectIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprint);

        var expected = new SprintDto { Id = sprint.Id };
        mapper.Map<SprintDto>(sprint).Returns(expected);

        var handler = new GetActiveSprintQueryHandler(repository, mapper);
        var query = new GetActiveSprintQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().Be(expected);
    }
}
