using BitirmeProject.IdentityService.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace BitirmeProject.IdentityService.Infrastructure.Security;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<string> _passwordHasher = new();

    public string HashPassword(string password)
    {
        // userId parametresine email/username geçebilirdik, şimdilik sabit string
        return _passwordHasher.HashPassword("user", password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword("user", hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success;
    }
}
