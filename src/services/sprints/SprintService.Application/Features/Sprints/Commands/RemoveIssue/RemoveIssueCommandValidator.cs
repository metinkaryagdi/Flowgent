using FluentValidation;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.RemoveIssue;

public sealed class RemoveIssueCommandValidator : AbstractValidator<RemoveIssueCommand>
{
    public RemoveIssueCommandValidator()
    {
        RuleFor(x => x.SprintId).NotEmpty();
        RuleFor(x => x.IssueId).NotEmpty();
        RuleFor(x => x.RemovedByUserId).NotEmpty();
    }
}
