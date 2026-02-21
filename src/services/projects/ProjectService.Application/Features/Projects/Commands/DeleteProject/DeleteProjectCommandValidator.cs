using FluentValidation;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
    }
}
