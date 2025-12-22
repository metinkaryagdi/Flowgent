using FluentValidation;

namespace BitirmeProject.IdentityService.Application.Features.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandValidator
    : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MinimumLength(2).WithMessage("Role name must be at least 2 characters.")
            .MaximumLength(50).WithMessage("Role name must be at most 50 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(200)
            .When(x => x.Description is not null && x.Description != string.Empty);
    }
}
