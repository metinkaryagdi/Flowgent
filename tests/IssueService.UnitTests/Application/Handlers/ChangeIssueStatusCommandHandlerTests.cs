using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.ChangeIssueStatus;
using BitirmeProject.IssueService.Application.ReadModels;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class ChangeIssueStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenIssueMissing()
    {
        var repository = Substitute.For<IIssueRepository>();
        var auditRepository = Substitute.For<IIssueAuditRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<ChangeIssueStatusCommandHandler>>();

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Issue?)null);

        var handler = new ChangeIssueStatusCommandHandler(repository, auditRepository, boardRepository, unitOfWork, outboxRepository, mapper, logger);
        var command = new ChangeIssueStatusCommand(Guid.NewGuid(), IssueStatus.InProgress, Guid.NewGuid(), 1, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await auditRepository.DidNotReceive().AddAsync(Arg.Any<IssueAudit>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenVersionConflict()
    {
        var repository = Substitute.For<IIssueRepository>();
        var auditRepository = Substitute.For<IIssueAuditRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<ChangeIssueStatusCommandHandler>>();

        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(issue);

        var handler = new ChangeIssueStatusCommandHandler(repository, auditRepository, boardRepository, unitOfWork, outboxRepository, mapper, logger);
        var command = new ChangeIssueStatusCommand(issue.Id, IssueStatus.InProgress, Guid.NewGuid(), ExpectedVersion: 99, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await auditRepository.DidNotReceive().AddAsync(Arg.Any<IssueAudit>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoOp_WhenStatusUnchanged()
    {
        var repository = Substitute.For<IIssueRepository>();
        var auditRepository = Substitute.For<IIssueAuditRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<ChangeIssueStatusCommandHandler>>();

        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(issue);

        var boardItem = new IssueBoardItem(issue);
        boardRepository.GetByIssueIdAsync(issue.Id, Arg.Any<CancellationToken>()).Returns(boardItem);

        var handler = new ChangeIssueStatusCommandHandler(repository, auditRepository, boardRepository, unitOfWork, outboxRepository, mapper, logger);
        var command = new ChangeIssueStatusCommand(issue.Id, IssueStatus.Open, Guid.NewGuid(), ExpectedVersion: issue.Version, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(issue.Id);
        result.Status.Should().Be(issue.Status);
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await auditRepository.DidNotReceive().AddAsync(Arg.Any<IssueAudit>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ChangesStatus_WritesOutbox_Audit_AndBoardItem()
    {
        var repository = Substitute.For<IIssueRepository>();
        var auditRepository = Substitute.For<IIssueAuditRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<ChangeIssueStatusCommandHandler>>();

        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(issue);
        boardRepository.GetByIssueIdAsync(issue.Id, Arg.Any<CancellationToken>()).Returns((IssueBoardItem?)null);

        var handler = new ChangeIssueStatusCommandHandler(repository, auditRepository, boardRepository, unitOfWork, outboxRepository, mapper, logger);
        var command = new ChangeIssueStatusCommand(issue.Id, IssueStatus.InProgress, Guid.NewGuid(), ExpectedVersion: issue.Version, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(issue.Id);
        result.Status.Should().Be(IssueStatus.InProgress);
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m => m.EventType == "IssueStatusChangedEvent"), Arg.Any<CancellationToken>());
        await auditRepository.Received(1).AddAsync(Arg.Any<IssueAudit>(), Arg.Any<CancellationToken>());
        await boardRepository.Received(1).AddAsync(Arg.Any<IssueBoardItem>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
