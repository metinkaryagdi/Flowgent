using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.StartSprint;
using BitirmeProject.SprintService.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace SprintService.UnitTests.Application.Handlers;

public sealed class StartSprintCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenSprintMissing()
    {
        var repository = Substitute.For<ISprintRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sprint?)null);

        var handler = new StartSprintCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new StartSprintCommand(Guid.NewGuid(), Guid.NewGuid(), null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenAnotherActiveSprintExists()
    {
        var repository = Substitute.For<ISprintRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var sprint = new Sprint(Guid.NewGuid(), "Sprint", null, Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprint);

        var anotherActive = new Sprint(sprint.ProjectId, "Active", null, Guid.NewGuid());
        repository.GetActiveByProjectIdAsync(sprint.ProjectId, Arg.Any<CancellationToken>()).Returns(anotherActive);

        var handler = new StartSprintCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new StartSprintCommand(sprint.Id, Guid.NewGuid(), null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StartsSprint_AndWritesOutbox()
    {
        var repository = Substitute.For<ISprintRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var sprint = new Sprint(Guid.NewGuid(), "Sprint", null, Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sprint);
        repository.GetActiveByProjectIdAsync(sprint.ProjectId, Arg.Any<CancellationToken>()).Returns((Sprint?)null);

        var expectedDto = new SprintDto { Id = sprint.Id };
        mapper.Map<SprintDto>(Arg.Any<Sprint>()).Returns(expectedDto);

        var handler = new StartSprintCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new StartSprintCommand(sprint.Id, Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedDto);
        await repository.Received(1).UpdateAsync(sprint, Arg.Any<CancellationToken>());
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m =>
            m.EventType == "SprintStartedEvent" &&
            !string.IsNullOrWhiteSpace(m.Payload)), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<SprintDto>(sprint);
    }
}
