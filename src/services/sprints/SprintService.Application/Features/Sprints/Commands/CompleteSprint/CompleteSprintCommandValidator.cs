using FluentValidation;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.CompleteSprint;

public sealed class CompleteSprintCommandValidator : AbstractValidator<CompleteSprintCommand>
{
    public CompleteSprintCommandValidator()
    {
        RuleFor(x => x.SprintId).NotEmpty();
        RuleFor(x => x.CompletedByUserId).NotEmpty();
    }
}
