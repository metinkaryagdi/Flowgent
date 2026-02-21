using FluentValidation;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Key).NotEmpty().MaximumLength(10);
        RuleFor(x => x.UpdatedByUserId).NotEmpty();
    }
}
