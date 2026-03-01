using BitirmeProject.IssueService.Application.Features.Issues.Commands.CreateIssue;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;

namespace IssueService.UnitTests.Application.Validators;

public sealed class CreateIssueCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenTitleEmpty()
    {
        var validator = new CreateIssueCommandValidator();
        var command = new CreateIssueCommand(Guid.NewGuid(), "", null, IssuePriority.Low, Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenTitleTooShort()
    {
        var validator = new CreateIssueCommandValidator();
        var command = new CreateIssueCommand(Guid.NewGuid(), "ab", null, IssuePriority.Low, Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenProjectIdMissing()
    {
        var validator = new CreateIssueCommandValidator();
        var command = new CreateIssueCommand(Guid.Empty, "Valid title", null, IssuePriority.Low, Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new CreateIssueCommandValidator();
        var command = new CreateIssueCommand(Guid.NewGuid(), "Valid title", null, IssuePriority.Low, Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
