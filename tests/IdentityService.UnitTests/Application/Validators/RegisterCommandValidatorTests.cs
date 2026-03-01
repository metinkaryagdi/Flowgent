using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;
using FluentAssertions;

namespace IdentityService.UnitTests.Application.Validators;

public sealed class RegisterCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenUserNameMissing()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("", "user@example.com", "Pass123!", "User", "Name");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenEmailMissing()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("user", "", "Pass123!", "User", "Name");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenPasswordMissing()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("user", "user@example.com", "", "User", "Name");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("user", "user@example.com", "Pass123!", "User", "Name");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
