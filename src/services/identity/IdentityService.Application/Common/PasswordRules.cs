using FluentValidation;

namespace BitirmeProject.IdentityService.Application.Common;

/// <summary>
/// Shared password policy for every path that sets a password (registration,
/// admin-created users, invite acceptance).
///
/// Deliberately NOT applied to login: raising the minimum there would lock out
/// existing accounts whose password predates this policy, and the login form
/// should not reveal the policy to an attacker anyway.
/// </summary>
public static class PasswordRules
{
    public const int MinimumLength = 12;

    public static IRuleBuilderOptions<T, string> ApplyPasswordPolicy<T>(
        this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(MinimumLength)
                .WithMessage($"Password must be at least {MinimumLength} characters.")
            .MaximumLength(128)
                .WithMessage("Password must be at most 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a special character.");
}
