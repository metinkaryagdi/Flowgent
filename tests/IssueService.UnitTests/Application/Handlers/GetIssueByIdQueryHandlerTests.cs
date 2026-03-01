using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueById;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class GetIssueByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsNull_WhenMissing()
    {
        var repository = Substitute.For<IIssueRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Issue?)null);

        var handler = new GetIssueByIdQueryHandler(repository, mapper);
        var query = new GetIssueByIdQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenFound()
    {
        var repository = Substitute.For<IIssueRepository>();
        var mapper = Substitute.For<IMapper>();

        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(issue);

        var expected = new IssueDto { Id = issue.Id };
        mapper.Map<IssueDto>(issue).Returns(expected);

        var handler = new GetIssueByIdQueryHandler(repository, mapper);
        var query = new GetIssueByIdQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().Be(expected);
    }
}
