using BitirmeProject.IssueService.Api.Events.Handlers;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Contracts.Events;

namespace IssueService.UnitTests.Consumers;

public sealed class IssueRemovedFromSprintEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_Ignores_WhenIssueMissing()
    {
        var issueRepository = Substitute.For<IIssueRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<IssueRemovedFromSprintEventHandler>>();

        issueRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Issue?)null);

        var handler = new IssueRemovedFromSprintEventHandler(issueRepository, boardRepository, unitOfWork, logger);
        var evt = new IssueRemovedFromSprintEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await issueRepository.DidNotReceive().UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
        await boardRepository.DidNotReceive().AddAsync(Arg.Any<IssueBoardItem>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenProjectMismatch()
    {
        var issueRepository = Substitute.For<IIssueRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<IssueRemovedFromSprintEventHandler>>();

        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        issueRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(issue);

        var handler = new IssueRemovedFromSprintEventHandler(issueRepository, boardRepository, unitOfWork, logger);
        var evt = new IssueRemovedFromSprintEvent(issue.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var act = async () => await handler.HandleAsync(evt, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await issueRepository.DidNotReceive().UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RemovesSprint_AndUpdatesBoard()
    {
        var issueRepository = Substitute.For<IIssueRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<IssueRemovedFromSprintEventHandler>>();

        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        issue.AssignToSprint(Guid.NewGuid());
        issueRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(issue);
        boardRepository.GetByIssueIdAsync(issue.Id, Arg.Any<CancellationToken>()).Returns((IssueBoardItem?)null);

        var handler = new IssueRemovedFromSprintEventHandler(issueRepository, boardRepository, unitOfWork, logger);
        var evt = new IssueRemovedFromSprintEvent(issue.Id, issue.ProjectId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await issueRepository.Received(1).UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
        await boardRepository.Received(1).AddAsync(Arg.Any<IssueBoardItem>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
