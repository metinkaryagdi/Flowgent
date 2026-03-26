using BitirmeProject.SprintService.Application.Features.Sprints.Commands.CreateSprint;
using FluentAssertions;

namespace SprintService.UnitTests.Application.Validators;

public sealed class CreateSprintCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenProjectMissing()
    {
        var validator = new CreateSprintCommandValidator();
        var command = new CreateSprintCommand(Guid.Empty, "Name", null, Guid.NewGuid(), null, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(14));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenNameEmpty()
    {
        var validator = new CreateSprintCommandValidator();
        var command = new CreateSprintCommand(Guid.NewGuid(), "", null, Guid.NewGuid(), null, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(14));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenCreatedByMissing()
    {
        var validator = new CreateSprintCommandValidator();
        var command = new CreateSprintCommand(Guid.NewGuid(), "Name", null, Guid.Empty, null, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(14));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new CreateSprintCommandValidator();
        var command = new CreateSprintCommand(Guid.NewGuid(), "Name", null, Guid.NewGuid(), null, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(14));

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenEndDateBeforeStartDate()
    {
        var validator = new CreateSprintCommandValidator();
        var startDate = DateTime.UtcNow.Date;
        var command = new CreateSprintCommand(Guid.NewGuid(), "Name", null, Guid.NewGuid(), null, startDate, startDate.AddDays(-1));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
