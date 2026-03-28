using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetBacklog;
using BitirmeProject.SprintService.Application.ReadModels;
using FluentAssertions;
using NSubstitute;

namespace SprintService.UnitTests.Application.Handlers;

public sealed class GetBacklogQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedList()
    {
        var repository = Substitute.For<ISprintIssueRepository>();
        var mapper = Substitute.For<IMapper>();

        var items = new List<SprintIssue>
        {
            new SprintIssue(Guid.NewGuid(), Guid.NewGuid(), "T1", "Task", "Low", "Open", Guid.NewGuid()),
            new SprintIssue(Guid.NewGuid(), Guid.NewGuid(), "T2", "Task", "High", "Done", Guid.NewGuid())
        };
        repository.GetBacklogByProjectIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(items);

        var dto1 = new SprintIssueDto { IssueId = items[0].IssueId };
        var dto2 = new SprintIssueDto { IssueId = items[1].IssueId };
        mapper.Map<SprintIssueDto>(items[0]).Returns(dto1);
        mapper.Map<SprintIssueDto>(items[1]).Returns(dto2);

        var handler = new GetBacklogQueryHandler(repository, mapper);
        var query = new GetBacklogQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainInOrder(dto1, dto2);
    }
}
