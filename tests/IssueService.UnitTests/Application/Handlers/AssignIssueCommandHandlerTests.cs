using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.AssignIssue;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class AssignIssueCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenIssueMissing()
    {
        var repository = Substitute.For<IIssueRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<AssignIssueCommandHandler>>();

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Issue?)null);

        var handler = new AssignIssueCommandHandler(repository, boardRepository, unitOfWork, outboxRepository, mapper, logger);
        var command = new AssignIssueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenVersionConflict()
    {
        var repository = Substitute.For<IIssueRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<AssignIssueCommandHandler>>();

        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(issue);

        var handler = new AssignIssueCommandHandler(repository, boardRepository, unitOfWork, outboxRepository, mapper, logger);
        var command = new AssignIssueCommand(issue.Id, Guid.NewGuid(), Guid.NewGuid(), expectedVersion: 99, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoOp_WhenAssigneeUnchanged()
    {
        var repository = Substitute.For<IIssueRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<AssignIssueCommandHandler>>();

        var assignee = Guid.NewGuid();
        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        issue.AssignTo(assignee);
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(issue);

        var boardItem = new IssueBoardItem(issue);
        boardRepository.GetByIssueIdAsync(issue.Id, Arg.Any<CancellationToken>()).Returns(boardItem);

        var handler = new AssignIssueCommandHandler(repository, boardRepository, unitOfWork, outboxRepository, mapper, logger);
        var command = new AssignIssueCommand(issue.Id, assignee, Guid.NewGuid(), expectedVersion: issue.Version, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(issue.Id);
        result.AssigneeUserId.Should().Be(assignee);
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AssignsIssue_WritesOutbox_AndBoard()
    {
        var repository = Substitute.For<IIssueRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<AssignIssueCommandHandler>>();

        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(issue);
        boardRepository.GetByIssueIdAsync(issue.Id, Arg.Any<CancellationToken>()).Returns((IssueBoardItem?)null);

        var handler = new AssignIssueCommandHandler(repository, boardRepository, unitOfWork, outboxRepository, mapper, logger);
        var command = new AssignIssueCommand(issue.Id, Guid.NewGuid(), Guid.NewGuid(), expectedVersion: issue.Version, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(issue.Id);
        result.AssigneeUserId.Should().Be(command.AssigneeUserId);
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m => m.EventType == "IssueAssignedEvent"), Arg.Any<CancellationToken>());
        await boardRepository.Received(1).AddAsync(Arg.Any<IssueBoardItem>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
