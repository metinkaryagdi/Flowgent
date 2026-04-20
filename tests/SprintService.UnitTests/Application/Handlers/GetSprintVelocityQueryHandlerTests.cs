using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintVelocity;
using BitirmeProject.SprintService.Application.ReadModels;
using BitirmeProject.SprintService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace SprintService.UnitTests.Application.Handlers;

public sealed class GetSprintVelocityQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCounts_FromIssues()
    {
        var sprintRepository = Substitute.For<ISprintRepository>();
        var issueRepository = Substitute.For<ISprintIssueRepository>();
        var summaryRepository = Substitute.For<ISprintSummaryRepository>();

        var startDate = DateTime.UtcNow.Date;
        var sprint = new Sprint(Guid.NewGuid(), "Sprint", null, startDate, startDate.AddDays(14), Guid.NewGuid());
        sprintRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprint);

        var items = new List<SprintIssue>
        {
            new SprintIssue(Guid.NewGuid(), Guid.NewGuid(), null, "T1", "Task", "Low", "Done", Guid.NewGuid()),
            new SprintIssue(Guid.NewGuid(), Guid.NewGuid(), null, "T2", "Task", "High", "InProgress", Guid.NewGuid()),
            new SprintIssue(Guid.NewGuid(), Guid.NewGuid(), null, "T3", "Task", "Medium", "done", Guid.NewGuid())
        };
        issueRepository.GetBySprintIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(items);

        var handler = new GetSprintVelocityQueryHandler(sprintRepository, issueRepository, summaryRepository);
        var query = new GetSprintVelocityQuery(sprint.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalIssues.Should().Be(3);
        result.DoneIssues.Should().Be(2);
    }
}
