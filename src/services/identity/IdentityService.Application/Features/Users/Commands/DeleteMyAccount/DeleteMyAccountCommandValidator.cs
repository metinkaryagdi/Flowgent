using FluentValidation;

namespace BitirmeProject.IdentityService.Application.Features.Users.Commands.DeleteMyAccount;

public sealed class DeleteMyAccountCommandValidator : AbstractValidator<DeleteMyAccountCommand>
{
    public DeleteMyAccountCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        // Deliberately no password-policy rules here. This confirms an existing password
        // rather than setting one, and rejecting it for failing today's policy would lock
        // older accounts out of deleting themselves.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password confirmation is required.");
    }
}
