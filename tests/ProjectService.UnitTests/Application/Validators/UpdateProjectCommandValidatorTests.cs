using BitirmeProject.ProjectService.Application.Features.Projects.Commands.UpdateProject;
using FluentAssertions;

namespace ProjectService.UnitTests.Application.Validators;

public sealed class UpdateProjectCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenIdMissing()
    {
        var validator = new UpdateProjectCommandValidator();
        var command = new UpdateProjectCommand(Guid.Empty, "Name", "KEY", Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenNameEmpty()
    {
        var validator = new UpdateProjectCommandValidator();
        var command = new UpdateProjectCommand(Guid.NewGuid(), "", "KEY", Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenKeyEmpty()
    {
        var validator = new UpdateProjectCommandValidator();
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", "", Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenUpdatedByMissing()
    {
        var validator = new UpdateProjectCommandValidator();
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", "KEY", Guid.Empty, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new UpdateProjectCommandValidator();
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", "KEY", Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
