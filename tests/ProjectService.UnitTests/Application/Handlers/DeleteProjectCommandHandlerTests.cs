using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.DeleteProject;
using BitirmeProject.ProjectService.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;

namespace ProjectService.UnitTests.Application.Handlers;

public sealed class DeleteProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenProjectMissing()
    {
        var repository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);

        var handler = new DeleteProjectCommandHandler(repository, unitOfWork, mapper);
        var command = new DeleteProjectCommand(Guid.NewGuid());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ArchivesProject_AndSaves()
    {
        var repository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();

        var project = new Project("Name", "KEY", Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(project);

        var expectedDto = new ProjectDto { Id = project.Id };
        mapper.Map<ProjectDto>(Arg.Any<Project>()).Returns(expectedDto);

        var handler = new DeleteProjectCommandHandler(repository, unitOfWork, mapper);
        var command = new DeleteProjectCommand(project.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedDto);
        await repository.Received(1).UpdateAsync(project, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<ProjectDto>(project);
    }
}
