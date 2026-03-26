using System.Security.Claims;

namespace Shared.Common.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to safely extract identity information.
/// Use these instead of reading userId/email from request body or route params.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the authenticated user's Id from JWT NameIdentifier or "sub" claim.
    /// Throws if the user is not authenticated or the claim is missing.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var id))
            throw new InvalidOperationException("Authenticated user does not have a valid UserId claim.");

        return id;
    }

    /// <summary>
    /// Returns the authenticated user's Id, or null if not authenticated.
    /// </summary>
    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub");

        return Guid.TryParse(value, out var id) ? id : null;
    }

    /// <summary>
    /// Returns the authenticated user's email from JWT Email claim.
    /// </summary>
    public static string? GetUserEmail(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");

    /// <summary>
    /// Returns all roles assigned to the authenticated user.
    /// </summary>
    public static IEnumerable<string> GetRoles(this ClaimsPrincipal user)
        => user.FindAll(ClaimTypes.Role).Select(c => c.Value);

    /// <summary>
    /// Returns true if the user has the specified role.
    /// </summary>
    public static bool HasRole(this ClaimsPrincipal user, string role)
        => user.IsInRole(role);
}
