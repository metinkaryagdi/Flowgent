using FluentValidation;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.AddIssue;

public sealed class AddIssueCommandValidator : AbstractValidator<AddIssueCommand>
{
    public AddIssueCommandValidator()
    {
        RuleFor(x => x.SprintId).NotEmpty();
        RuleFor(x => x.IssueId).NotEmpty();
        RuleFor(x => x.AddedByUserId).NotEmpty();
    }
}
