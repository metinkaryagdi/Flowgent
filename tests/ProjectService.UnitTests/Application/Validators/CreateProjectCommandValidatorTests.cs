using BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;
using FluentAssertions;

namespace ProjectService.UnitTests.Application.Validators;

public sealed class CreateProjectCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenNameEmpty()
    {
        var validator = new CreateProjectCommandValidator();
        var command = new CreateProjectCommand("", "KEY", Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenKeyEmpty()
    {
        var validator = new CreateProjectCommandValidator();
        var command = new CreateProjectCommand("Name", "", Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenOwnerMissing()
    {
        var validator = new CreateProjectCommandValidator();
        var command = new CreateProjectCommand("Name", "KEY", Guid.Empty, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new CreateProjectCommandValidator();
        var command = new CreateProjectCommand("Name", "KEY", Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
