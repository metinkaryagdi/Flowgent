using BitirmeProject.SprintService.Application.Features.Sprints.Commands.StartSprint;
using FluentAssertions;

namespace SprintService.UnitTests.Application.Validators;

public sealed class StartSprintCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenSprintMissing()
    {
        var validator = new StartSprintCommandValidator();
        var command = new StartSprintCommand(Guid.Empty, Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenStartedByMissing()
    {
        var validator = new StartSprintCommandValidator();
        var command = new StartSprintCommand(Guid.NewGuid(), Guid.Empty, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new StartSprintCommandValidator();
        var command = new StartSprintCommand(Guid.NewGuid(), Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
