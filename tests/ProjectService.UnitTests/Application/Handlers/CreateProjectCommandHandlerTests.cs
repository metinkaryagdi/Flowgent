using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;
using BitirmeProject.ProjectService.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace ProjectService.UnitTests.Application.Handlers;

public sealed class CreateProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenKeyExists()
    {
        var repository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.ExistsByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateProjectCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new CreateProjectCommand("Name", "KEY", Guid.NewGuid(), null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await repository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreatesProject_AndOutbox()
    {
        var repository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.ExistsByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        Project? capturedProject = null;
        repository.AddAsync(Arg.Do<Project>(x => capturedProject = x), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var expectedDto = new ProjectDto { Id = Guid.NewGuid() };
        mapper.Map<ProjectDto>(Arg.Any<Project>()).Returns(expectedDto);

        var handler = new CreateProjectCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new CreateProjectCommand("Name", "KEY", Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedDto);
        capturedProject.Should().NotBeNull();
        capturedProject!.Name.Should().Be(command.Name);
        capturedProject.Key.Should().Be(command.Key);

        await repository.Received(1).AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m =>
            m.EventType == "ProjectCreatedEvent" &&
            !string.IsNullOrWhiteSpace(m.Payload)), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<ProjectDto>(Arg.Any<Project>());
    }
}
