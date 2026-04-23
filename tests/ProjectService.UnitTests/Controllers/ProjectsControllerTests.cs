using System.Security.Claims;
using BitirmeProject.ProjectService.Api.Controllers;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.AddMember;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.UpdateProject;
using BitirmeProject.ProjectService.Domain.Entities;
using BitirmeProject.ProjectService.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace ProjectService.UnitTests.Controllers;

public sealed class ProjectsControllerTests
{
    private static ControllerContext MakeContext(Guid userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, "Test");
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
    }

    [Fact]
    public async Task Create_UsesOwnerUserIdFromClaims()
    {
        var mediator = Substitute.For<IMediator>();
        var projectRepository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var claimsUserId = Guid.NewGuid();
        var controller = new ProjectsController(mediator, projectRepository, unitOfWork)
        {
            ControllerContext = MakeContext(claimsUserId)
        };
        // Body sends a DIFFERENT OwnerUserId — it must be overridden by claims
        var commandFromBody = new CreateProjectCommand("Name", "KEY", Guid.NewGuid(), null);
        var dto = new ProjectDto { Id = Guid.NewGuid(), Name = commandFromBody.Name, Key = commandFromBody.Key };
        mediator.Send(Arg.Any<CreateProjectCommand>()).Returns(dto);

        var result = await controller.Create(commandFromBody);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(ProjectsController.GetById));
        // The command sent to mediator must use the claims userId, not the body one
        await mediator.Received(1).Send(
            Arg.Is<CreateProjectCommand>(c => c.OwnerUserId == claimsUserId));
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var mediator = Substitute.For<IMediator>();
        var projectRepository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var controller = new ProjectsController(mediator, projectRepository, unitOfWork);
        var id = Guid.NewGuid();
        projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Project?)null);
        mediator.Send(Arg.Any<BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectById.GetProjectByIdQuery>())
            .Returns((ProjectDto?)null);

        var result = await controller.GetById(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_UsesRouteId()
    {
        var mediator = Substitute.For<IMediator>();
        var projectRepository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ownerUserId = Guid.NewGuid();
        var project = new Project("Name", "KEY", ownerUserId);
        projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(project);
        var controller = new ProjectsController(mediator, projectRepository, unitOfWork)
        {
            ControllerContext = MakeContext(ownerUserId)
        };
        var routeId = Guid.NewGuid();
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", "KEY", Guid.NewGuid(), null);
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
        var projectRepository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userId = Guid.NewGuid();
        var project = new Project("Name", "KEY", userId);
        projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(project);
        var controller = new ProjectsController(mediator, projectRepository, unitOfWork)
        {
            ControllerContext = MakeContext(userId)
        };
        var projectId = Guid.NewGuid();
        var command = new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, ProjectMemberRole.Member);
        var dto = new ProjectDto { Id = projectId };
        mediator.Send(Arg.Any<AddMemberCommand>()).Returns(dto);

        var result = await controller.AddMember(projectId, command);

        result.Result.Should().BeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Is<AddMemberCommand>(c =>
            c.ProjectId == projectId &&
            c.AddedByUserId == userId));
    }

    [Fact]
    public async Task HardDelete_RemovesProjectAndSaves_WhenAdmin()
    {
        var mediator = Substitute.For<IMediator>();
        var projectRepository = Substitute.For<IProjectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var adminUserId = Guid.NewGuid();
        var project = new Project("Name", "KEY", Guid.NewGuid());
        projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var controller = new ProjectsController(mediator, projectRepository, unitOfWork)
        {
            ControllerContext = MakeContext(adminUserId, "Admin")
        };

        var result = await controller.HardDelete(project.Id);

        result.Should().BeOfType<NoContentResult>();
        await projectRepository.Received(1).DeleteAsync(project, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
