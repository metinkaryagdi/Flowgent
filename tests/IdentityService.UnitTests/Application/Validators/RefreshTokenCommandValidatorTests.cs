using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Refresh;
using FluentAssertions;

namespace IdentityService.UnitTests.Application.Validators;

public sealed class RefreshTokenCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenTokenMissing()
    {
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand("");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand("token");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
