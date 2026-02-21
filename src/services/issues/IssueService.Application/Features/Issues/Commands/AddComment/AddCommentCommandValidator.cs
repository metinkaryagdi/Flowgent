using FluentValidation;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AddComment;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.IssueId).NotEmpty();
        RuleFor(x => x.AuthorUserId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}
