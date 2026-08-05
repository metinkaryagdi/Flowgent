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

namespace IdentityService.IntegrationTests.Auth;

/// <summary>
/// Covers the email-verification gate on self-service registration: an account starts
/// Pending, cannot sign in, and only becomes usable after the emailed single-use token is
/// redeemed. Before this existed anyone could register with any address, so these tests
/// guard the thing that actually closes open-registration abuse.
/// </summary>
public sealed class EmailVerificationTests : IClassFixture<IdentityWebAppFactory>
{
    private const string Password = "VerifyMe123!";

    private readonly HttpClient _client;
    private readonly IdentityWebAppFactory _factory;

    public EmailVerificationTests(IdentityWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static (string UserName, string Email) NewIdentity(string prefix)
        => ($"{prefix}_{Guid.NewGuid():N}", $"{prefix}_{Guid.NewGuid():N}@example.com");

    private Task<HttpResponseMessage> RegisterAsync(string userName, string email)
        => _client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            UserName = userName,
            Email = email,
            Password
        });

    private Task<HttpResponseMessage> LoginAsync(string email)
        => _client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            UserNameOrEmail = email,
            Password
        });

    private async Task<(UserStatus Status, DateTime? VerifiedAt)> ReadUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);
        return (user.Status, user.EmailVerifiedAt);
    }

    [Fact]
    public async Task Register_CreatesPendingAccount_AndSendsVerificationEmail()
    {
        var (userName, email) = NewIdentity("pending");

        var response = await RegisterAsync(userName, email);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var (status, verifiedAt) = await ReadUserAsync(email);
        status.Should().Be(UserStatus.Pending);
        verifiedAt.Should().BeNull();

        _factory.Emails.GetLatestVerificationToken(email).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_BeforeVerification_Returns401WithEmailNotVerifiedCode()
    {
        var (userName, email) = NewIdentity("unverified");
        await RegisterAsync(userName, email);

        var response = await LoginAsync(email);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        // The stable code is what the web client branches on to offer a resend link;
        // matching on the human-readable message would break on any wording change.
        body!.Code.Should().Be("email_not_verified");
        body.Email.Should().Be(email);
    }

    [Fact]
    public async Task VerifyEmail_WithValidToken_ActivatesAccountAndAllowsLogin()
    {
        var (userName, email) = NewIdentity("verify");
        await RegisterAsync(userName, email);

        var token = _factory.Emails.GetLatestVerificationToken(email);
        var verify = await _client.PostAsJsonAsync("/api/v1/identity/verify-email", new { Token = token });

        verify.StatusCode.Should().Be(HttpStatusCode.OK);

        var (status, verifiedAt) = await ReadUserAsync(email);
        status.Should().Be(UserStatus.Active);
        verifiedAt.Should().NotBeNull();

        var login = await LoginAsync(email);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VerifyEmail_WithSameTokenTwice_IsRejectedTheSecondTime()
    {
        var (userName, email) = NewIdentity("replay");
        await RegisterAsync(userName, email);

        var token = _factory.Emails.GetLatestVerificationToken(email);

        var first = await _client.PostAsJsonAsync("/api/v1/identity/verify-email", new { Token = token });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Single-use: a link forwarded to someone else must not work a second time.
        var second = await _client.PostAsJsonAsync("/api/v1/identity/verify-email", new { Token = token });
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyEmail_WithUnknownToken_IsRejected()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/identity/verify-email",
            new { Token = "not-a-real-token-value" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResendVerification_IssuesNewToken_AndRetiresThePreviousOne()
    {
        var (userName, email) = NewIdentity("resend");
        await RegisterAsync(userName, email);

        var firstToken = _factory.Emails.GetLatestVerificationToken(email);

        var resend = await _client.PostAsJsonAsync("/api/v1/identity/resend-verification", new { Email = email });
        resend.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondToken = _factory.Emails.GetLatestVerificationToken(email);
        secondToken.Should().NotBe(firstToken);

        // The superseded link must stop working, otherwise "resend" would leave every
        // previously mailed token live indefinitely.
        var oldTokenAttempt = await _client.PostAsJsonAsync(
            "/api/v1/identity/verify-email", new { Token = firstToken });
        oldTokenAttempt.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var newTokenAttempt = await _client.PostAsJsonAsync(
            "/api/v1/identity/verify-email", new { Token = secondToken });
        newTokenAttempt.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendVerification_ForUnknownAddress_StillReturns200AndSendsNothing()
    {
        var unknown = $"nobody_{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/v1/identity/resend-verification", new { Email = unknown });

        // Must not reveal whether the address is registered -- otherwise this endpoint is
        // an account-enumeration oracle for any address list.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.Emails.VerificationEmailCount(unknown).Should().Be(0);
    }

    [Fact]
    public async Task ResendVerification_ForAlreadyVerifiedAccount_SendsNothing()
    {
        var (userName, email) = NewIdentity("already");
        await _factory.RegisterAndVerifyAsync(_client, userName, email, Password);

        var countAfterVerify = _factory.Emails.VerificationEmailCount(email);

        var response = await _client.PostAsJsonAsync("/api/v1/identity/resend-verification", new { Email = email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.Emails.VerificationEmailCount(email).Should().Be(countAfterVerify);
    }

    private sealed class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
