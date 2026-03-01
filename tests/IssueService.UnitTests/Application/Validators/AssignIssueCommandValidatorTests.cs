using BitirmeProject.IssueService.Application.Features.Issues.Commands.AssignIssue;
using FluentAssertions;

namespace IssueService.UnitTests.Application.Validators;

public sealed class AssignIssueCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenIssueMissing()
    {
        var validator = new AssignIssueCommandValidator();
        var command = new AssignIssueCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 1, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenAssigneeMissing()
    {
        var validator = new AssignIssueCommandValidator();
        var command = new AssignIssueCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 1, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenAssignedByMissing()
    {
        var validator = new AssignIssueCommandValidator();
        var command = new AssignIssueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 1, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenExpectedVersionInvalid()
    {
        var validator = new AssignIssueCommandValidator();
        var command = new AssignIssueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new AssignIssueCommandValidator();
        var command = new AssignIssueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
