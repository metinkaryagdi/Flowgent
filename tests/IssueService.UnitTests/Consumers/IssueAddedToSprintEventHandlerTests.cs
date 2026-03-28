using BitirmeProject.IssueService.Api.Events.Handlers;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.ReadModels;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Contracts.Events;

namespace IssueService.UnitTests.Consumers;

public sealed class IssueAddedToSprintEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_Ignores_WhenIssueMissing()
    {
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<IssueAddedToSprintEventHandler>>();

        boardRepository.GetByIssueIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((IssueBoardItem?)null);

        var handler = new IssueAddedToSprintEventHandler(boardRepository, unitOfWork, logger);
        var evt = new IssueAddedToSprintEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await boardRepository.DidNotReceive().UpdateAsync(Arg.Any<IssueBoardItem>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenProjectMismatch()
    {
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<IssueAddedToSprintEventHandler>>();

        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        var boardItem = new IssueBoardItem(issue);
        boardRepository.GetByIssueIdAsync(issue.Id, Arg.Any<CancellationToken>()).Returns(boardItem);

        var handler = new IssueAddedToSprintEventHandler(boardRepository, unitOfWork, logger);
        var evt = new IssueAddedToSprintEvent(issue.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var act = async () => await handler.HandleAsync(evt, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AssignsSprint_AndUpdatesBoard()
    {
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<IssueAddedToSprintEventHandler>>();

        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        var boardItem = new IssueBoardItem(issue);
        boardRepository.GetByIssueIdAsync(issue.Id, Arg.Any<CancellationToken>()).Returns(boardItem);

        var handler = new IssueAddedToSprintEventHandler(boardRepository, unitOfWork, logger);
        var evt = new IssueAddedToSprintEvent(issue.Id, issue.ProjectId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        boardItem.SprintId.Should().Be(evt.SprintId);
        await boardRepository.Received(1).UpdateAsync(boardItem, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
