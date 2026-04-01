using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ProjectService.IntegrationTests.Helpers;

public static class JwtTestHelper
{
    private const string Secret = "YourSuperSecretKeyForJwtTokenGenerationMinimum32Characters";
    private const string Issuer = "BitirmeProject.IdentityService";
    private const string Audience = "BitirmeProject.Clients";

    public static string GenerateToken(Guid userId, string email, IEnumerable<string>? roles = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
        };

        foreach (var role in roles ?? Array.Empty<string>())
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static HttpClient WithJwt(this HttpClient client, Guid userId, string email, IEnumerable<string>? roles = null)
    {
        var token = GenerateToken(userId, email, roles);
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", $"accessToken={token}");
        return client;
    }
}
