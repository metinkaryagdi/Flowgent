namespace BitirmeProject.IdentityService.Application.DTOs;

/// <summary>
/// Result of a self-service registration.
///
/// Deliberately carries no tokens: the account is created in
/// <see cref="Domain.Enums.UserStatus.Pending"/> and cannot sign in until the emailed
/// verification link is followed.
/// </summary>
public sealed class RegisterResponseDto
{
    public Guid UserId { get; init; }

    /// <summary>Where the verification link was sent, so the client can display it.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Always true today; kept explicit so the client branches on data rather
    /// than on the absence of a token.</summary>
    public bool VerificationRequired { get; init; } = true;
}
