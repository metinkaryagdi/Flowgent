using FluentValidation;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.CreateIssue;

public sealed class CreateIssueCommandValidator : AbstractValidator<CreateIssueCommand>
{
    public CreateIssueCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3);
        RuleFor(x => x.CreatedByUserId).NotEmpty();
    }
}