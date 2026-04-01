using System.Security.Claims;
using BitirmeProject.SprintService.Api.Controllers;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.AddIssue;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.CreateSprint;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.StartSprint;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace SprintService.UnitTests.Controllers;

public sealed class SprintsControllerTests
{
    private static ControllerContext MakeContext()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }, "Test");
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
    }

    [Fact]
    public async Task Create_ReturnsOk_WithResult()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new SprintsController(mediator) { ControllerContext = MakeContext() };
        var startDate = DateTime.UtcNow.Date;
        var command = new CreateSprintCommand(Guid.NewGuid(), "Sprint 1", null, Guid.NewGuid(), null, startDate, startDate.AddDays(14));
        var dto = new SprintDto { Id = Guid.NewGuid(), Name = command.Name };
        mediator.Send(Arg.Any<CreateSprintCommand>()).Returns(dto);

        var result = await controller.Create(command);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Start_UsesRouteId()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new SprintsController(mediator)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var sprintId = Guid.NewGuid();
        var dto = new SprintDto { Id = sprintId };
        mediator.Send(Arg.Any<StartSprintCommand>()).Returns(dto);

        var result = await controller.Start(sprintId);

        result.Result.Should().BeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Is<StartSprintCommand>(c => c.SprintId == sprintId));
    }

    [Fact]
    public async Task AddIssue_UsesRouteId()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new SprintsController(mediator)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var sprintId = Guid.NewGuid();
        var command = new AddIssueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);
        var dto = new SprintIssueDto { SprintId = sprintId };
        mediator.Send(Arg.Any<AddIssueCommand>()).Returns(dto);

        var result = await controller.AddIssue(sprintId, command);

        result.Result.Should().BeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Is<AddIssueCommand>(c => c.SprintId == sprintId));
    }
}
