using FluentValidation;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AssignIssue;

public sealed class AssignIssueCommandValidator : AbstractValidator<AssignIssueCommand>
{
    public AssignIssueCommandValidator()
    {
        RuleFor(x => x.IssueId).NotEmpty();
        RuleFor(x => x.AssigneeUserId).NotEmpty();
        RuleFor(x => x.AssignedByUserId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
