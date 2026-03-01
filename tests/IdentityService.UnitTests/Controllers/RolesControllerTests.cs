using BitirmeProject.IdentityService.Api.Controllers;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Roles.Commands.CreateRole;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IdentityService.UnitTests.Controllers;

public sealed class RolesControllerTests
{
    [Fact]
    public async Task Create_ReturnsOk_WithResult()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new RolesController(mediator);
        var command = new CreateRoleCommand("Admin");
        var dto = new RoleDto { Id = Guid.NewGuid(), Name = command.Name };
        mediator.Send(command).Returns(dto);

        var result = await controller.Create(command);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }
}
