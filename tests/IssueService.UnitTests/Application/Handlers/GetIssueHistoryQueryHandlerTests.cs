using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueHistory;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class GetIssueHistoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedList()
    {
        var repository = Substitute.For<IIssueAuditRepository>();
        var mapper = Substitute.For<IMapper>();

        var issueId = Guid.NewGuid();
        var items = new List<IssueAudit>
        {
            new IssueAudit(issueId, IssueStatus.Open, IssueStatus.InProgress, Guid.NewGuid()),
            new IssueAudit(issueId, IssueStatus.InProgress, IssueStatus.Done, Guid.NewGuid())
        };
        repository.GetByIssueIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(items);

        var dto1 = new IssueAuditDto { IssueId = issueId };
        var dto2 = new IssueAuditDto { IssueId = issueId };
        mapper.Map<IssueAuditDto>(items[0]).Returns(dto1);
        mapper.Map<IssueAuditDto>(items[1]).Returns(dto2);

        var handler = new GetIssueHistoryQueryHandler(repository, mapper);
        var query = new GetIssueHistoryQuery(issueId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainInOrder(dto1, dto2);
    }
}
