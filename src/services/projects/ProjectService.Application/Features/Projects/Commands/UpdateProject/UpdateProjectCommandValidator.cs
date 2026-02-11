using FluentValidation;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3);

        RuleFor(x => x.Key)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(10);

        RuleFor(x => x.UpdatedByUserId)
            .NotEmpty();
    }
}
