using FluentValidation;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Key).NotEmpty().MaximumLength(10);
        RuleFor(x => x.OwnerUserId).NotEmpty();
    }
}
