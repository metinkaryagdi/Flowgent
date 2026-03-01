using BitirmeProject.ProjectService.Application.Features.Projects.Commands.DeleteProject;
using FluentAssertions;

namespace ProjectService.UnitTests.Application.Validators;

public sealed class DeleteProjectCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenProjectMissing()
    {
        var validator = new DeleteProjectCommandValidator();
        var command = new DeleteProjectCommand(Guid.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new DeleteProjectCommandValidator();
        var command = new DeleteProjectCommand(Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
