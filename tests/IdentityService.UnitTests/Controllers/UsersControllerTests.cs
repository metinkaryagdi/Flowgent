using BitirmeProject.IdentityService.Api.Controllers;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.RegisterUser;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.UpdateUser;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IdentityService.UnitTests.Controllers;

public sealed class UsersControllerTests
{
    [Fact]
    public async Task Register_ReturnsCreatedAtAction()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new UsersController(mediator);
        var command = new RegisterUserCommand("user@example.com", "Pass123!", "User", "Name");
        var dto = new UserDto { Id = Guid.NewGuid(), Email = command.Email };
        mediator.Send(command).Returns(dto);

        var result = await controller.Register(command);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(UsersController.GetById));
        created.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new UsersController(mediator);
        mediator.Send(Arg.Any<BitirmeProject.IdentityService.Application.Features.Users.Queries.GetUserById.GetUserByIdQuery>())
            .Returns((UserDto?)null);

        var result = await controller.GetById(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_UsesRouteId()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new UsersController(mediator);
        var routeId = Guid.NewGuid();
        var command = new UpdateUserCommand(Guid.NewGuid(), "user@example.com", "User", "Name");
        var dto = new UserDto { Id = routeId };
        mediator.Send(Arg.Any<UpdateUserCommand>()).Returns(dto);

        var result = await controller.UpdateUser(routeId, command);

        result.Result.Should().BeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Is<UpdateUserCommand>(c => c.Id == routeId));
    }

    [Fact]
    public async Task DeleteUser_ReturnsNoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new UsersController(mediator);

        var result = await controller.DeleteUser(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
        await mediator.Received(1).Send(Arg.Any<IdentityService.Application.Features.Users.Commands.DeleteUser.DeleteUserCommand>());
    }
}
