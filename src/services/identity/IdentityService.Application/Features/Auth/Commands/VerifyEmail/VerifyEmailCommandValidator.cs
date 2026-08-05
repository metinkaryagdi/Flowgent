using FluentValidation;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required.")
            .MaximumLength(200).WithMessage("Verification token is not valid.");
    }
}
