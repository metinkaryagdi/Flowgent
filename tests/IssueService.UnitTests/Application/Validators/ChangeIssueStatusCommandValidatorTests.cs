using BitirmeProject.IssueService.Application.Features.Issues.Commands.ChangeIssueStatus;
using FluentAssertions;

namespace IssueService.UnitTests.Application.Validators;

public sealed class ChangeIssueStatusCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenIssueMissing()
    {
        var validator = new ChangeIssueStatusCommandValidator();
        var command = new ChangeIssueStatusCommand(Guid.Empty, BitirmeProject.IssueService.Domain.Enums.IssueStatus.Open, Guid.NewGuid(), 1, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenChangedByMissing()
    {
        var validator = new ChangeIssueStatusCommandValidator();
        var command = new ChangeIssueStatusCommand(Guid.NewGuid(), BitirmeProject.IssueService.Domain.Enums.IssueStatus.Open, Guid.Empty, 1, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenExpectedVersionInvalid()
    {
        var validator = new ChangeIssueStatusCommandValidator();
        var command = new ChangeIssueStatusCommand(Guid.NewGuid(), BitirmeProject.IssueService.Domain.Enums.IssueStatus.Open, Guid.NewGuid(), 0, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new ChangeIssueStatusCommandValidator();
        var command = new ChangeIssueStatusCommand(Guid.NewGuid(), BitirmeProject.IssueService.Domain.Enums.IssueStatus.Open, Guid.NewGuid(), 1, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
