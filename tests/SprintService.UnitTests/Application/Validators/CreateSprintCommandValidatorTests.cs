using BitirmeProject.SprintService.Application.Features.Sprints.Commands.CreateSprint;
using FluentAssertions;

namespace SprintService.UnitTests.Application.Validators;

public sealed class CreateSprintCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenProjectMissing()
    {
        var validator = new CreateSprintCommandValidator();
        var command = new CreateSprintCommand(Guid.Empty, "Name", null, Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenNameEmpty()
    {
        var validator = new CreateSprintCommandValidator();
        var command = new CreateSprintCommand(Guid.NewGuid(), "", null, Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenCreatedByMissing()
    {
        var validator = new CreateSprintCommandValidator();
        var command = new CreateSprintCommand(Guid.NewGuid(), "Name", null, Guid.Empty, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new CreateSprintCommandValidator();
        var command = new CreateSprintCommand(Guid.NewGuid(), "Name", null, Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
