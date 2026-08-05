using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IdentityService.IntegrationTests.Fixtures;

namespace IdentityService.IntegrationTests.Helpers;

/// <summary>
/// Registration now creates a Pending account that cannot sign in, so every test that
/// needs a usable account has to complete verification first. This walks the real flow --
/// register, read the token out of the captured email, POST it to verify-email -- rather
/// than shortcutting through the database, so the helper itself fails if the flow breaks.
/// </summary>
public static class RegistrationHelper
{
    public static async Task RegisterAndVerifyAsync(
        this IdentityWebAppFactory factory,
        HttpClient client,
        string userName,
        string email,
        string password)
    {
        var register = await client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            UserName = userName,
            Email = email,
            Password = password
        });

        register.StatusCode.Should().Be(HttpStatusCode.OK, "registration should succeed");

        var token = factory.Emails.GetLatestVerificationToken(email);
        token.Should().NotBeNullOrWhiteSpace("registration must send a verification email");

        var verify = await client.PostAsJsonAsync("/api/v1/identity/verify-email", new { Token = token });
        verify.StatusCode.Should().Be(HttpStatusCode.OK, "the freshly issued token should be accepted");
    }
}
