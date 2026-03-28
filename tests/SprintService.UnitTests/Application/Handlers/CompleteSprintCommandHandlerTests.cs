using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.CompleteSprint;
using BitirmeProject.SprintService.Application.ReadModels;
using BitirmeProject.SprintService.Domain.Entities;
using BitirmeProject.SprintService.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace SprintService.UnitTests.Application.Handlers;

public sealed class CompleteSprintCommandHandlerTests
{
    private static Sprint CreateSprint()
    {
        var startDate = DateTime.UtcNow.Date;
        return new Sprint(Guid.NewGuid(), "Sprint", null, startDate, startDate.AddDays(14), Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_Throws_WhenSprintMissing()
    {
        var repository = Substitute.For<ISprintRepository>();
        var issueRepository = Substitute.For<ISprintIssueRepository>();
        var summaryRepository = Substitute.For<ISprintSummaryRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sprint?)null);

        var handler = new CompleteSprintCommandHandler(repository, issueRepository, summaryRepository, unitOfWork, outboxRepository, mapper);
        var command = new CompleteSprintCommand(Guid.NewGuid(), Guid.NewGuid(), null, SprintCarryOverPolicy.Backlog, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CompletesSprint_AndWritesOutbox()
    {
        var repository = Substitute.For<ISprintRepository>();
        var issueRepository = Substitute.For<ISprintIssueRepository>();
        var summaryRepository = Substitute.For<ISprintSummaryRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var sprint = CreateSprint();
        sprint.Start();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprint);
        issueRepository.GetBySprintIdAsync(sprint.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SprintIssue>());

        var expectedDto = new SprintDto { Id = sprint.Id };
        mapper.Map<SprintDto>(Arg.Any<Sprint>()).Returns(expectedDto);

        var handler = new CompleteSprintCommandHandler(repository, issueRepository, summaryRepository, unitOfWork, outboxRepository, mapper);
        var command = new CompleteSprintCommand(sprint.Id, Guid.NewGuid(), Guid.NewGuid(), SprintCarryOverPolicy.Backlog, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedDto);
        await repository.Received(1).UpdateAsync(sprint, Arg.Any<CancellationToken>());
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m =>
            m.EventType == "SprintCompletedEvent" &&
            !string.IsNullOrWhiteSpace(m.Payload)), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<SprintDto>(sprint);
    }

    [Fact]
    public async Task Handle_Throws_WhenManualPolicyHasIncompleteIssues()
    {
        var repository = Substitute.For<ISprintRepository>();
        var issueRepository = Substitute.For<ISprintIssueRepository>();
        var summaryRepository = Substitute.For<ISprintSummaryRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var sprint = CreateSprint();
        sprint.Start();
        repository.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        issueRepository.GetBySprintIdAsync(sprint.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { new SprintIssue(Guid.NewGuid(), sprint.ProjectId, "Title", "Task", "Low", "Open", Guid.NewGuid()) });

        var handler = new CompleteSprintCommandHandler(repository, issueRepository, summaryRepository, unitOfWork, outboxRepository, mapper);
        var command = new CompleteSprintCommand(sprint.Id, Guid.NewGuid(), null, SprintCarryOverPolicy.Manual, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
