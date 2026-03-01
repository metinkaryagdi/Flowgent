using BitirmeProject.ProjectService.Application.Features.Projects.Commands.RemoveMember;
using FluentAssertions;

namespace ProjectService.UnitTests.Application.Validators;

public sealed class RemoveMemberCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenProjectMissing()
    {
        var validator = new RemoveMemberCommandValidator();
        var command = new RemoveMemberCommand(Guid.Empty, Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenUserMissing()
    {
        var validator = new RemoveMemberCommandValidator();
        var command = new RemoveMemberCommand(Guid.NewGuid(), Guid.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new RemoveMemberCommandValidator();
        var command = new RemoveMemberCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
