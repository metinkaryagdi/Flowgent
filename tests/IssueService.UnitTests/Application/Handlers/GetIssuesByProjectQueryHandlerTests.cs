using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProject;
using BitirmeProject.IssueService.Application.ReadModels;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class GetIssuesByProjectQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedList()
    {
        var repository = Substitute.For<IIssueBoardRepository>();
        var mapper = Substitute.For<IMapper>();

        var issue1 = new Issue(Guid.NewGuid(), "T1", null, IssuePriority.Low, Guid.NewGuid());
        var issue2 = new Issue(Guid.NewGuid(), "T2", null, IssuePriority.High, Guid.NewGuid());
        var items = new List<IssueBoardItem>
        {
            new IssueBoardItem(issue1),
            new IssueBoardItem(issue2)
        };
        repository.GetByProjectIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(items);

        var dto1 = new IssueBoardItemDto { IssueId = issue1.Id };
        var dto2 = new IssueBoardItemDto { IssueId = issue2.Id };
        mapper.Map<IssueBoardItemDto>(items[0]).Returns(dto1);
        mapper.Map<IssueBoardItemDto>(items[1]).Returns(dto2);

        var cache = Substitute.For<IDistributedCache>();
        var handler = new GetIssuesByProjectQueryHandler(repository, mapper, cache);
        var query = new GetIssuesByProjectQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainInOrder(dto1, dto2);
    }
}
