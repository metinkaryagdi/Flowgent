using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintVelocity;
using BitirmeProject.SprintService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace SprintService.UnitTests.Application.Handlers;

public sealed class GetSprintVelocityQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCounts_FromIssues()
    {
        var repository = Substitute.For<ISprintIssueRepository>();

        var items = new List<SprintIssue>
        {
            new SprintIssue(Guid.NewGuid(), Guid.NewGuid(), "T1", "Task", "Low", "Done", Guid.NewGuid()),
            new SprintIssue(Guid.NewGuid(), Guid.NewGuid(), "T2", "Task", "High", "InProgress", Guid.NewGuid()),
            new SprintIssue(Guid.NewGuid(), Guid.NewGuid(), "T3", "Task", "Medium", "done", Guid.NewGuid())
        };
        repository.GetBySprintIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(items);

        var handler = new GetSprintVelocityQueryHandler(repository);
        var query = new GetSprintVelocityQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalIssues.Should().Be(3);
        result.DoneIssues.Should().Be(2);
    }
}
