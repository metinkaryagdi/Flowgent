using System.Security.Cryptography;
using System.Text;

namespace BitirmeProject.IdentityService.Application.Common;

/// <summary>
/// Provides SHA-256 hashing for refresh tokens.
/// Raw tokens are sent to the client via HttpOnly cookie;
/// only the hash is persisted in the database.
/// </summary>
public static class TokenHasher
{
    public static string Hash(string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
