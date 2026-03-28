using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesBySprint;
using BitirmeProject.IssueService.Application.ReadModels;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class GetIssuesBySprintQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedList()
    {
        var repository = Substitute.For<IIssueRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var mapper = Substitute.For<IMapper>();
        var sprintId = Guid.NewGuid();

        var items = new List<Issue>
        {
            new Issue(Guid.NewGuid(), "T1", null, IssuePriority.Low, Guid.NewGuid()),
            new Issue(Guid.NewGuid(), "T2", null, IssuePriority.High, Guid.NewGuid())
        };
        repository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(items);
        boardRepository.GetBySprintIdAsync(sprintId, Arg.Any<CancellationToken>())
            .Returns(items.Select(i =>
            {
                var boardItem = new IssueBoardItem(i);
                boardItem.SprintId = sprintId;
                return boardItem;
            }).ToList());

        var handler = new GetIssuesBySprintQueryHandler(repository, boardRepository, mapper);
        var query = new GetIssuesBySprintQuery(sprintId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(items[0].Id, items[1].Id);
        result.Select(x => x.SprintId).Should().OnlyContain(x => x == sprintId);
    }
}
