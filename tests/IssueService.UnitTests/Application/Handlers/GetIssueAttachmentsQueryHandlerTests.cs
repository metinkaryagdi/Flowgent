using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueAttachments;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class GetIssueAttachmentsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedList()
    {
        var repository = Substitute.For<IIssueAttachmentRepository>();
        var mapper = Substitute.For<IMapper>();

        var issueId = Guid.NewGuid();
        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        var items = new List<IssueAttachment>
        {
            new IssueAttachment(issueId, Guid.NewGuid(), "file1.txt", "text/plain", 100, Guid.NewGuid()),
            new IssueAttachment(issueId, Guid.NewGuid(), "file2.txt", "text/plain", 200, Guid.NewGuid())
        };
        repository.GetByIssueIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(items);

        var dto1 = new IssueAttachmentDto { IssueId = issueId };
        var dto2 = new IssueAttachmentDto { IssueId = issueId };
        mapper.Map<IssueAttachmentDto>(items[0]).Returns(dto1);
        mapper.Map<IssueAttachmentDto>(items[1]).Returns(dto2);

        var handler = new GetIssueAttachmentsQueryHandler(repository, mapper);
        var query = new GetIssueAttachmentsQuery(issueId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainInOrder(dto1, dto2);
    }
}
