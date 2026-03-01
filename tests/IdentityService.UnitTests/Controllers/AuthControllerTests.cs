using BitirmeProject.IdentityService.Api.Controllers;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Login;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IdentityService.UnitTests.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_ReturnsOk_WithResult()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new AuthController(mediator);
        var command = new RegisterCommand("user@example.com", "Pass123!", "User", "Name");
        var dto = new AuthResponseDto { AccessToken = "token" };
        mediator.Send(command).Returns(dto);

        var result = await controller.Register(command);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Login_ReturnsOk_WithResult()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new AuthController(mediator);
        var command = new LoginCommand("user@example.com", "Pass123!");
        var dto = new AuthResponseDto { AccessToken = "token" };
        mediator.Send(command).Returns(dto);

        var result = await controller.Login(command);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }
}
