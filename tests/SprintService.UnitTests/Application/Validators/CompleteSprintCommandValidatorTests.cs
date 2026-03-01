using BitirmeProject.SprintService.Application.Features.Sprints.Commands.CompleteSprint;
using FluentAssertions;

namespace SprintService.UnitTests.Application.Validators;

public sealed class CompleteSprintCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenSprintMissing()
    {
        var validator = new CompleteSprintCommandValidator();
        var command = new CompleteSprintCommand(Guid.Empty, Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenCompletedByMissing()
    {
        var validator = new CompleteSprintCommandValidator();
        var command = new CompleteSprintCommand(Guid.NewGuid(), Guid.Empty, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new CompleteSprintCommandValidator();
        var command = new CompleteSprintCommand(Guid.NewGuid(), Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
