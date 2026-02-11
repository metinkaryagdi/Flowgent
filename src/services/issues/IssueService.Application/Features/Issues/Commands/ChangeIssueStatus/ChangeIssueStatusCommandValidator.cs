using FluentValidation;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.ChangeIssueStatus;

public sealed class ChangeIssueStatusCommandValidator : AbstractValidator<ChangeIssueStatusCommand>
{
    public ChangeIssueStatusCommandValidator()
    {
        RuleFor(x => x.IssueId).NotEmpty();
        RuleFor(x => x.ChangedByUserId).NotEmpty();
    }
}