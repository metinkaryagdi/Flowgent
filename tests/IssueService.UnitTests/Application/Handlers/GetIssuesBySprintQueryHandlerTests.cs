using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesBySprint;
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
        var mapper = Substitute.For<IMapper>();

        var items = new List<Issue>
        {
            new Issue(Guid.NewGuid(), "T1", null, IssuePriority.Low, Guid.NewGuid()),
            new Issue(Guid.NewGuid(), "T2", null, IssuePriority.High, Guid.NewGuid())
        };
        repository.GetBySprintIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(items);

        var dto1 = new IssueDto { Id = items[0].Id };
        var dto2 = new IssueDto { Id = items[1].Id };
        mapper.Map<IssueDto>(items[0]).Returns(dto1);
        mapper.Map<IssueDto>(items[1]).Returns(dto2);

        var handler = new GetIssuesBySprintQueryHandler(repository, mapper);
        var query = new GetIssuesBySprintQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainInOrder(dto1, dto2);
    }
}
