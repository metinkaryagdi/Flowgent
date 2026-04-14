using System.Security.Claims;
using BitirmeProject.IdentityService.Api.Controllers;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.RegisterUser;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.UpdateUser;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IdentityService.UnitTests.Controllers;

public sealed class UsersControllerTests
{
    private static ControllerContext MakeAdminContext()
    {
        var adminId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test");
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
    }

    [Fact]
    public async Task Register_ReturnsCreatedAtAction()
    {
        var mediator = Substitute.For<IMediator>();
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var organizationRepository = Substitute.For<IOrganizationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var controller = new UsersController(mediator, userRepository, roleRepository, organizationRepository, unitOfWork);
        var command = new RegisterUserCommand("user", "user@example.com", "Pass123!");
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
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var organizationRepository = Substitute.For<IOrganizationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var controller = new UsersController(mediator, userRepository, roleRepository, organizationRepository, unitOfWork)
        {
            ControllerContext = MakeAdminContext()
        };
        mediator.Send(Arg.Any<BitirmeProject.IdentityService.Application.Features.Users.Queries.GetUserById.GetUserByIdQuery>())
            .Returns((UserDto?)null);

        var result = await controller.GetById(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_UsesRouteId()
    {
        var mediator = Substitute.For<IMediator>();
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var organizationRepository = Substitute.For<IOrganizationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var controller = new UsersController(mediator, userRepository, roleRepository, organizationRepository, unitOfWork)
        {
            ControllerContext = MakeAdminContext()
        };
        var routeId = Guid.NewGuid();
        var command = new UpdateUserCommand(Guid.NewGuid(), "user", "user@example.com");
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
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var organizationRepository = Substitute.For<IOrganizationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var controller = new UsersController(mediator, userRepository, roleRepository, organizationRepository, unitOfWork);

        var result = await controller.DeleteUser(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
        await mediator.Received(1).Send(Arg.Any<IdentityService.Application.Features.Users.Commands.DeleteUser.DeleteUserCommand>());
    }
}
