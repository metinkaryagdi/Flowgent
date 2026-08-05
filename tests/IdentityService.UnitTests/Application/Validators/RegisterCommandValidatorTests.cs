using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;
using FluentAssertions;

namespace IdentityService.UnitTests.Application.Validators;

public sealed class RegisterCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenUserNameMissing()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("", "user@example.com", "Pass123!");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenEmailMissing()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("user", "", "Pass123!");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenPasswordMissing()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("user", "user@example.com", "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("user", "user@example.com", "ValidPass123!");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Pass123!", "shorter than the 12 character minimum")]
    [InlineData("alllowercase123!", "no uppercase letter")]
    [InlineData("ALLUPPERCASE123!", "no lowercase letter")]
    [InlineData("NoDigitsHere!!!!", "no digit")]
    [InlineData("NoSpecialChar123", "no special character")]
    public void Validate_Fails_WhenPasswordViolatesPolicy(string password, string reason)
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("user", "user@example.com", password);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse(reason);
    }
}
