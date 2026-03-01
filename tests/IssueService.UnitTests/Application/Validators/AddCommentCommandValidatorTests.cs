using BitirmeProject.IssueService.Application.Features.Issues.Commands.AddComment;
using FluentAssertions;

namespace IssueService.UnitTests.Application.Validators;

public sealed class AddCommentCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenIssueMissing()
    {
        var validator = new AddCommentCommandValidator();
        var command = new AddCommentCommand(Guid.Empty, Guid.NewGuid(), "hi", null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenAuthorMissing()
    {
        var validator = new AddCommentCommandValidator();
        var command = new AddCommentCommand(Guid.NewGuid(), Guid.Empty, "hi", null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenContentEmpty()
    {
        var validator = new AddCommentCommandValidator();
        var command = new AddCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "", null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new AddCommentCommandValidator();
        var command = new AddCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "hi", null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
