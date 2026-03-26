using BitirmeProject.SprintService.Domain.Enums;
using FluentValidation;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.CompleteSprint;

public sealed class CompleteSprintCommandValidator : AbstractValidator<CompleteSprintCommand>
{
    public CompleteSprintCommandValidator()
    {
        RuleFor(x => x.SprintId).NotEmpty();
        RuleFor(x => x.CompletedByUserId).NotEmpty();
        RuleFor(x => x.CarryOverPolicy).IsInEnum();

        When(x => x.CarryOverPolicy == SprintCarryOverPolicy.NextSprint, () =>
        {
            RuleFor(x => x.NextSprintId).NotEmpty();
        });
    }
}
