using BitirmeProject.ProjectService.Api.Controllers;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.AddMember;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.UpdateProject;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace ProjectService.UnitTests.Controllers;

public sealed class ProjectsControllerTests
{
    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithResult()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new ProjectsController(mediator);
        var command = new CreateProjectCommand("Name", "KEY", Guid.NewGuid(), null);
        var dto = new ProjectDto { Id = Guid.NewGuid(), Name = command.Name, Key = command.Key };
        mediator.Send(command).Returns(dto);

        var result = await controller.Create(command);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(ProjectsController.GetById));
        created.RouteValues.Should().ContainKey("id");
        created.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new ProjectsController(mediator);
        var id = Guid.NewGuid();
        mediator.Send(Arg.Any<BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectById.GetProjectByIdQuery>())
            .Returns((ProjectDto?)null);

        var result = await controller.GetById(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_UsesRouteId()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new ProjectsController(mediator);
        var routeId = Guid.NewGuid();
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", "KEY");
        var dto = new ProjectDto { Id = routeId };
        mediator.Send(Arg.Any<UpdateProjectCommand>()).Returns(dto);

        var result = await controller.Update(routeId, command);

        result.Result.Should().BeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Is<UpdateProjectCommand>(c => c.Id == routeId));
    }

    [Fact]
    public async Task AddMember_UsesRouteProjectId()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new ProjectsController(mediator);
        var projectId = Guid.NewGuid();
        var command = new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid());
        var dto = new ProjectDto { Id = projectId };
        mediator.Send(Arg.Any<AddMemberCommand>()).Returns(dto);

        var result = await controller.AddMember(projectId, command);

        result.Result.Should().BeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Is<AddMemberCommand>(c => c.ProjectId == projectId));
    }
}
