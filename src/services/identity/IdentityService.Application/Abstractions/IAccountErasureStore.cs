namespace BitirmeProject.IdentityService.Application.Abstractions;

/// <summary>
/// Deletes the rows that hang off a user account and would otherwise survive erasure.
///
/// These are hard deletes, not soft ones: refresh tokens, verification tokens and
/// pending invites are credentials and addressing data with no historical value, and a
/// pending invite in particular still carries the raw email address in its own column.
/// Memberships go too, so a deleted account stops appearing in organization member lists.
/// </summary>
public interface IAccountErasureStore
{
    /// <summary>
    /// Removes the user's sessions, tokens, role assignments and memberships, plus any
    /// invite still addressed to <paramref name="email"/>. Stages the deletes on the
    /// caller's unit of work where possible; returns the number of rows removed.
    /// </summary>
    Task<int> PurgeAccountArtifactsAsync(Guid userId, string email, CancellationToken cancellationToken = default);
}
