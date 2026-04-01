using FluentValidation;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required.")
            .MinimumLength(2).WithMessage("Organization name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Organization name must be at most 100 characters.");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("CreatedByUserId is required.");
    }
}
