using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.UpdateProject;
using BitirmeProject.ProjectService.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace ProjectService.UnitTests.Application.Handlers;

public sealed class UpdateProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenProjectMissing()
    {
        var repository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);

        var handler = new UpdateProjectCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", "KEY", Guid.NewGuid(), null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenKeyAlreadyExists()
    {
        var repository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var project = new Project("Old", "OLD", Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(project);
        repository.ExistsByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new UpdateProjectCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new UpdateProjectCommand(project.Id, "Name", "NEW", Guid.NewGuid(), null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdatesProject_AndWritesTwoOutboxMessages()
    {
        var repository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var project = new Project("Old", "OLD", Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(project);
        repository.ExistsByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var expectedDto = new ProjectDto { Id = project.Id };
        mapper.Map<ProjectDto>(Arg.Any<Project>()).Returns(expectedDto);

        var handler = new UpdateProjectCommandHandler(repository, unitOfWork, outboxRepository, mapper);
        var command = new UpdateProjectCommand(project.Id, "New Name", "NEW", Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedDto);
        project.Name.Should().Be("New Name");
        project.Key.Should().Be("NEW");

        await repository.Received(1).UpdateAsync(project, Arg.Any<CancellationToken>());
        await outboxRepository.Received(2).AddAsync(Arg.Is<OutboxMessage>(m =>
            m.EventType is "ProjectUpdatedEvent" or "ProjectSettingsUpdatedEvent" &&
            !string.IsNullOrWhiteSpace(m.Payload)), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<ProjectDto>(project);
    }
}
