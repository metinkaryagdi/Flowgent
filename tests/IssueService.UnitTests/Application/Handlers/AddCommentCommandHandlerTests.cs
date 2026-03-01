using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.AddComment;
using BitirmeProject.IssueService.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class AddCommentCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenIssueMissing()
    {
        var issueRepository = Substitute.For<IIssueRepository>();
        var commentRepository = Substitute.For<IIssueCommentRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        issueRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Issue?)null);

        var handler = new AddCommentCommandHandler(issueRepository, commentRepository, unitOfWork, outboxRepository, mapper);
        var command = new AddCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "hi", null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await commentRepository.DidNotReceive().AddAsync(Arg.Any<IssueComment>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddsComment_AndWritesOutbox()
    {
        var issueRepository = Substitute.For<IIssueRepository>();
        var commentRepository = Substitute.For<IIssueCommentRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var issue = new Issue(Guid.NewGuid(), "Title", null, BitirmeProject.IssueService.Domain.Enums.IssuePriority.Low, Guid.NewGuid());
        issueRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(issue);

        IssueComment? captured = null;
        commentRepository.AddAsync(Arg.Do<IssueComment>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var expectedDto = new IssueCommentDto { Id = Guid.NewGuid() };
        mapper.Map<IssueCommentDto>(Arg.Any<IssueComment>()).Returns(expectedDto);

        var handler = new AddCommentCommandHandler(issueRepository, commentRepository, unitOfWork, outboxRepository, mapper);
        var command = new AddCommentCommand(issue.Id, Guid.NewGuid(), "hello", Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedDto);
        captured.Should().NotBeNull();
        captured!.IssueId.Should().Be(issue.Id);

        await commentRepository.Received(1).AddAsync(Arg.Any<IssueComment>(), Arg.Any<CancellationToken>());
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m =>
            m.EventType == "CommentAddedEvent" &&
            !string.IsNullOrWhiteSpace(m.Payload)), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<IssueCommentDto>(Arg.Any<IssueComment>());
    }
}
