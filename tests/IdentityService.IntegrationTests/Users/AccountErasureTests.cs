using Xunit;
using System.Net;
using System.Net.Http.Json;
using BitirmeProject.IdentityService.Domain.Enums;
using BitirmeProject.IdentityService.Infrastructure.Persistence;
using FluentAssertions;
using IdentityService.IntegrationTests.Fixtures;
using IdentityService.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.IntegrationTests.Users;

/// <summary>
/// Covers self-service account erasure. The point of these tests is the difference between
/// "the row is flagged deleted" and "the personal data is gone" -- the admin delete does
/// the first and cannot answer a KVKK/GDPR deletion request, so the assertions here are
/// about the columns themselves, not about a status field.
/// </summary>
public sealed class AccountErasureTests : IClassFixture<IdentityWebAppFactory>
{
    /// <summary>Must clear PasswordRules.MinimumLength (12) or registration 400s.</summary>
    private const string Password = "EraseMe1234!";

    private readonly HttpClient _client;
    private readonly IdentityWebAppFactory _factory;

    public AccountErasureTests(IdentityWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static (string UserName, string Email) NewIdentity(string prefix)
        => ($"{prefix}_{Guid.NewGuid():N}", $"{prefix}_{Guid.NewGuid():N}@example.com");

    private async Task<HttpClient> SignedInClientAsync(string userName, string email)
    {
        var client = _factory.CreateClient();
        await _factory.RegisterAndVerifyAsync(client, userName, email, Password);

        var login = await client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            UserNameOrEmail = email,
            Password
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        return client;
    }

    [Fact]
    public async Task DeleteMyAccount_RemovesPersonalDataFromTheRow()
    {
        var (userName, email) = NewIdentity("erase");
        var client = await SignedInClientAsync(userName, email);

        var response = await client.PostAsJsonAsync("/api/v1/identity/users/me/delete", new { Password });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        // IgnoreQueryFilters: the row is soft-deleted, so the default filter hides it --
        // and the whole question here is what is left *in* that hidden row.
        var user = await db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(u => u.Id != Guid.Empty && u.UserName.StartsWith("deleted_"));

        user.Email.Should().NotBe(email, "the address must not survive an erasure request");
        user.Email.Should().EndWith("@deleted.invalid");
        user.UserName.Should().NotBe(userName);
        user.IsDeleted.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Deactivated);

        // Nothing anywhere should still be able to find the account by its old address.
        var byOldAddress = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email);
        byOldAddress.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMyAccount_WrongPassword_IsRefusedAndChangesNothing()
    {
        var (userName, email) = NewIdentity("wrongpw");
        var client = await SignedInClientAsync(userName, email);

        var response = await client.PostAsJsonAsync(
            "/api/v1/identity/users/me/delete",
            new { Password = "NotThePassword1234!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);

        user.IsDeleted.Should().BeFalse("a failed confirmation must not delete anything");
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task DeleteMyAccount_RemovesTokensAndMemberships()
    {
        var (userName, email) = NewIdentity("artifacts");
        var client = await SignedInClientAsync(userName, email);

        // Give the account something to clean up beyond the login session.
        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/identity/organizations",
            new { Name = $"Erase Org {Guid.NewGuid():N}" });
        createOrg.IsSuccessStatusCode.Should().BeTrue();

        Guid userId;
        using (var before = _factory.Services.CreateScope())
        {
            var db = before.ServiceProvider.GetRequiredService<IdentityDbContext>();
            userId = (await db.Users.AsNoTracking().SingleAsync(u => u.Email == email)).Id;

            (await db.RefreshTokens.IgnoreQueryFilters().AnyAsync(t => t.UserId == userId))
                .Should().BeTrue("signing in should have issued a refresh token to clean up");
            (await db.OrganizationMembers.IgnoreQueryFilters().AnyAsync(m => m.UserId == userId))
                .Should().BeTrue("creating an organization should have added a membership");
        }

        var response = await client.PostAsJsonAsync("/api/v1/identity/users/me/delete", new { Password });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var after = _factory.Services.CreateScope();
        var db2 = after.ServiceProvider.GetRequiredService<IdentityDbContext>();

        (await db2.RefreshTokens.IgnoreQueryFilters().AnyAsync(t => t.UserId == userId))
            .Should().BeFalse("sessions are credentials, they are hard deleted");
        (await db2.EmailVerificationTokens.IgnoreQueryFilters().AnyAsync(t => t.UserId == userId))
            .Should().BeFalse("verification tokens carry the address in their own column");
        (await db2.OrganizationMembers.IgnoreQueryFilters().AnyAsync(m => m.UserId == userId))
            .Should().BeFalse("a deleted account must stop appearing in member lists");
        (await db2.InviteTokens.IgnoreQueryFilters().AnyAsync(i => i.Email == email))
            .Should().BeFalse("a pending invite still holds the raw address");
    }

    [Fact]
    public async Task DeleteMyAccount_EndsTheSession()
    {
        var (userName, email) = NewIdentity("session");
        var client = await SignedInClientAsync(userName, email);

        (await client.GetAsync("/api/v1/identity/organizations/my/all"))
            .StatusCode.Should().Be(HttpStatusCode.OK, "the session should work before deletion");

        var response = await client.PostAsJsonAsync("/api/v1/identity/users/me/delete", new { Password });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The stamp rotates on anonymisation, so the token the client still holds is dead
        // even though it has not expired.
        (await client.GetAsync("/api/v1/identity/organizations/my/all"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteMyAccount_RequiresAuthentication()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync(
            "/api/v1/identity/users/me/delete",
            new { Password });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
