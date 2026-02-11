using FluentValidation;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UserNameOrEmail)
            .NotEmpty()
            .MinimumLength(3);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6);
    }
}