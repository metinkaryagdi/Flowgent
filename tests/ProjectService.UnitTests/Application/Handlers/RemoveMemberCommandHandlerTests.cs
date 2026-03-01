using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.RemoveMember;
using BitirmeProject.ProjectService.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;

namespace ProjectService.UnitTests.Application.Handlers;

public sealed class RemoveMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenProjectMissing()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();

        projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);

        var handler = new RemoveMemberCommandHandler(projectRepository, memberRepository, unitOfWork, mapper);
        var command = new RemoveMemberCommand(Guid.NewGuid(), Guid.NewGuid());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await memberRepository.DidNotReceive().RemoveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RemovesMember_AndSaves()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();

        var project = new Project("Name", "KEY", Guid.NewGuid());
        projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(project);

        var expectedDto = new ProjectDto { Id = project.Id };
        mapper.Map<ProjectDto>(Arg.Any<Project>()).Returns(expectedDto);

        var handler = new RemoveMemberCommandHandler(projectRepository, memberRepository, unitOfWork, mapper);
        var command = new RemoveMemberCommand(project.Id, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedDto);
        await memberRepository.Received(1).RemoveAsync(project.Id, command.UserId, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<ProjectDto>(project);
    }
}
