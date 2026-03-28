using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Login;
using FluentAssertions;

namespace IdentityService.UnitTests.Application.Validators;

public sealed class LoginCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenUserNameMissing()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("", "pass");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenPasswordMissing()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("user", "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("user", "password123");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
