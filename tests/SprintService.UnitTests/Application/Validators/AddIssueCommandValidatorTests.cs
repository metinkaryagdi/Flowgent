using BitirmeProject.SprintService.Application.Features.Sprints.Commands.AddIssue;
using FluentAssertions;

namespace SprintService.UnitTests.Application.Validators;

public sealed class AddIssueCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenSprintMissing()
    {
        var validator = new AddIssueCommandValidator();
        var command = new AddIssueCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenIssueMissing()
    {
        var validator = new AddIssueCommandValidator();
        var command = new AddIssueCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenAddedByMissing()
    {
        var validator = new AddIssueCommandValidator();
        var command = new AddIssueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new AddIssueCommandValidator();
        var command = new AddIssueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
