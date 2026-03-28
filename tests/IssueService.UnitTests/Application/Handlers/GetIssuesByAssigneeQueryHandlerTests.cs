using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByAssignee;
using BitirmeProject.IssueService.Application.ReadModels;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class GetIssuesByAssigneeQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedList()
    {
        var repository = Substitute.For<IIssueRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var mapper = Substitute.For<IMapper>();

        var items = new List<Issue>
        {
            new Issue(Guid.NewGuid(), "T1", null, IssuePriority.Low, Guid.NewGuid()),
            new Issue(Guid.NewGuid(), "T2", null, IssuePriority.High, Guid.NewGuid())
        };
        repository.GetByAssigneeAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(items);
        boardRepository.GetByIssueIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(items.Select(i => new IssueBoardItem(i)).ToList());

        var handler = new GetIssuesByAssigneeQueryHandler(repository, boardRepository, mapper);
        var query = new GetIssuesByAssigneeQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(items[0].Id, items[1].Id);
    }
}
