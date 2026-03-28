using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.RemoveIssue;
using BitirmeProject.SprintService.Application.ReadModels;
using BitirmeProject.SprintService.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace SprintService.UnitTests.Application.Handlers;

public sealed class RemoveIssueCommandHandlerTests
{
    private static Sprint CreateSprint()
    {
        var startDate = DateTime.UtcNow.Date;
        return new Sprint(Guid.NewGuid(), "Sprint", null, startDate, startDate.AddDays(14), Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_Throws_WhenSprintMissing()
    {
        var sprintRepository = Substitute.For<ISprintRepository>();
        var issueRepository = Substitute.For<ISprintIssueRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        sprintRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sprint?)null);

        var handler = new RemoveIssueCommandHandler(sprintRepository, issueRepository, unitOfWork, outboxRepository, mapper);
        var command = new RemoveIssueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await issueRepository.DidNotReceive().UpdateAsync(Arg.Any<SprintIssue>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenSprintIssueMissing()
    {
        var sprintRepository = Substitute.For<ISprintRepository>();
        var issueRepository = Substitute.For<ISprintIssueRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var sprint = CreateSprint();
        sprintRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprint);
        issueRepository.GetByIssueIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SprintIssue?)null);

        var handler = new RemoveIssueCommandHandler(sprintRepository, issueRepository, unitOfWork, outboxRepository, mapper);
        var command = new RemoveIssueCommand(sprint.Id, Guid.NewGuid(), Guid.NewGuid(), null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await issueRepository.DidNotReceive().UpdateAsync(Arg.Any<SprintIssue>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenIssueNotInSprint()
    {
        var sprintRepository = Substitute.For<ISprintRepository>();
        var issueRepository = Substitute.For<ISprintIssueRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var sprint = CreateSprint();
        sprintRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprint);

        var sprintIssue = new SprintIssue(Guid.NewGuid(), sprint.ProjectId, "Title", "Task", "Low", "Open", Guid.NewGuid());
        sprintIssue.SprintId = Guid.NewGuid();
        issueRepository.GetByIssueIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprintIssue);

        var handler = new RemoveIssueCommandHandler(sprintRepository, issueRepository, unitOfWork, outboxRepository, mapper);
        var command = new RemoveIssueCommand(sprint.Id, sprintIssue.IssueId, Guid.NewGuid(), null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await issueRepository.DidNotReceive().UpdateAsync(Arg.Any<SprintIssue>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RemovesIssue_AndWritesOutbox()
    {
        var sprintRepository = Substitute.For<ISprintRepository>();
        var issueRepository = Substitute.For<ISprintIssueRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var sprint = CreateSprint();
        sprintRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprint);

        var sprintIssue = new SprintIssue(Guid.NewGuid(), sprint.ProjectId, "Title", "Task", "Low", "Open", Guid.NewGuid());
        sprintIssue.SprintId = sprint.Id;
        issueRepository.GetByIssueIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprintIssue);

        var expectedDto = new SprintIssueDto { SprintId = sprint.Id };
        mapper.Map<SprintIssueDto>(Arg.Any<SprintIssue>()).Returns(expectedDto);

        var handler = new RemoveIssueCommandHandler(sprintRepository, issueRepository, unitOfWork, outboxRepository, mapper);
        var command = new RemoveIssueCommand(sprint.Id, sprintIssue.IssueId, Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedDto);
        await issueRepository.Received(1).UpdateAsync(sprintIssue, Arg.Any<CancellationToken>());
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m =>
            m.EventType == "IssueRemovedFromSprintEvent" &&
            !string.IsNullOrWhiteSpace(m.Payload)), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<SprintIssueDto>(sprintIssue);
    }

    [Fact]
    public async Task Handle_Throws_WhenSprintCompleted()
    {
        var sprintRepository = Substitute.For<ISprintRepository>();
        var issueRepository = Substitute.For<ISprintIssueRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var sprint = CreateSprint();
        sprint.Start();
        sprint.Complete();
        sprintRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprint);

        var handler = new RemoveIssueCommandHandler(sprintRepository, issueRepository, unitOfWork, outboxRepository, mapper);
        var command = new RemoveIssueCommand(sprint.Id, Guid.NewGuid(), Guid.NewGuid(), null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await issueRepository.DidNotReceive().UpdateAsync(Arg.Any<SprintIssue>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
