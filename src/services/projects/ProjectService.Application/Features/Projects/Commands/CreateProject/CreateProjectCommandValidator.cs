using FluentValidation;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3);

        RuleFor(x => x.Key)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(10);

        RuleFor(x => x.OwnerUserId)
            .NotEmpty();
    }
}