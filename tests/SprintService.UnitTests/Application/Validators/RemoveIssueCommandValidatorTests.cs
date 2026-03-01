using BitirmeProject.SprintService.Application.Features.Sprints.Commands.RemoveIssue;
using FluentAssertions;

namespace SprintService.UnitTests.Application.Validators;

public sealed class RemoveIssueCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenSprintMissing()
    {
        var validator = new RemoveIssueCommandValidator();
        var command = new RemoveIssueCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenIssueMissing()
    {
        var validator = new RemoveIssueCommandValidator();
        var command = new RemoveIssueCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenRemovedByMissing()
    {
        var validator = new RemoveIssueCommandValidator();
        var command = new RemoveIssueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new RemoveIssueCommandValidator();
        var command = new RemoveIssueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
