using BitirmeProject.IdentityService.Api.Controllers;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Login;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IdentityService.UnitTests.Controllers;

public sealed class AuthControllerTests
{
    private static ControllerContext MakeContext()
    {
        return new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task Register_ReturnsOk_WithResult()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new AuthController(mediator) { ControllerContext = MakeContext() };
        var command = new RegisterCommand("user", "user@example.com", "Pass123!");
        var dto = new AuthResponseDto { AccessToken = "token", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        mediator.Send(Arg.Any<RegisterCommand>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await controller.Register(command);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_ReturnsOk_WithResult()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new AuthController(mediator) { ControllerContext = MakeContext() };
        var command = new LoginCommand("user@example.com", "Pass123!");
        var dto = new AuthResponseDto { AccessToken = "token", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        mediator.Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await controller.Login(command);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }
}
