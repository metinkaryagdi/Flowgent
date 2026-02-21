using FluentValidation;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.StartSprint;

public sealed class StartSprintCommandValidator : AbstractValidator<StartSprintCommand>
{
    public StartSprintCommandValidator()
    {
        RuleFor(x => x.SprintId).NotEmpty();
        RuleFor(x => x.StartedByUserId).NotEmpty();
    }
}
