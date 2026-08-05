using Xunit;
using System.Net;
using System.Net.Http.Json;
using BitirmeProject.IdentityService.Infrastructure.Persistence;
using FluentAssertions;
using IdentityService.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.IntegrationTests.Auth;

/// <summary>
/// Covers the account-lockout wiring in LoginCommandHandler: five consecutive failed
/// attempts lock the account for 15 minutes, and a successful login resets the counter.
/// The lockout fields existed on the User entity long before the handler used them, so
/// these tests exist to keep the handler from silently drifting back to dead code.
/// </summary>
public sealed class AccountLockoutTests : IClassFixture<IdentityWebAppFactory>
{
    private const string ValidPassword = "Correct1234!";
    private const string WrongPassword = "WrongPassword1!";

    private readonly HttpClient _client;
    private readonly IdentityWebAppFactory _factory;

    public AccountLockoutTests(IdentityWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterUserAsync()
    {
        var email = $"lockout_{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            UserName = $"lockout_{Guid.NewGuid():N}",
            Email = email,
            Password = ValidPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return email;
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password)
        => _client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            UserNameOrEmail = email,
            Password = password
        });

    private async Task<(int FailedCount, DateTime? LockoutEnd)> ReadLockoutStateAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);
        return (user.FailedLoginCount, user.LockoutEnd);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_IsNotAffectedByLockoutWiring()
    {
        var email = await RegisterUserAsync();

        var response = await LoginAsync(email, ValidPassword);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "the lockout check must not break an ordinary successful login");

        var (failedCount, lockoutEnd) = await ReadLockoutStateAsync(email);
        failedCount.Should().Be(0);
        lockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task FourFailedAttempts_DoNotLockTheAccount()
    {
        var email = await RegisterUserAsync();

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var failed = await LoginAsync(email, WrongPassword);
            failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var (failedCount, lockoutEnd) = await ReadLockoutStateAsync(email);
        failedCount.Should().Be(4);
        lockoutEnd.Should().BeNull("the account must stay usable below the threshold");

        // The correct password still works on the fifth attempt.
        var success = await LoginAsync(email, ValidPassword);
        success.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FiveFailedAttempts_LockTheAccountForFifteenMinutes()
    {
        var email = await RegisterUserAsync();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var failed = await LoginAsync(email, WrongPassword);
            failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var (failedCount, lockoutEnd) = await ReadLockoutStateAsync(email);
        failedCount.Should().Be(5);
        lockoutEnd.Should().NotBeNull("five failures must trigger a lockout");
        lockoutEnd!.Value.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));

        // Even the correct password is refused while the lockout is active, and the
        // response must be a 401 carrying the machine-readable lockout code so the
        // client can explain the failure instead of showing "wrong password".
        var lockedOut = await LoginAsync(email, ValidPassword);
        lockedOut.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var payload = await lockedOut.Content.ReadFromJsonAsync<ErrorResponse>();
        payload.Should().NotBeNull();
        payload!.Code.Should().Be("account_locked");
        payload.LockoutEnd.Should().NotBeNull();
    }

    [Fact]
    public async Task SuccessfulLogin_ResetsTheFailedAttemptCounter()
    {
        var email = await RegisterUserAsync();

        for (var attempt = 1; attempt <= 3; attempt++)
            await LoginAsync(email, WrongPassword);

        var (beforeCount, _) = await ReadLockoutStateAsync(email);
        beforeCount.Should().Be(3);

        var success = await LoginAsync(email, ValidPassword);
        success.StatusCode.Should().Be(HttpStatusCode.OK);

        var (afterCount, afterLockoutEnd) = await ReadLockoutStateAsync(email);
        afterCount.Should().Be(0, "a successful login must clear the failure counter");
        afterLockoutEnd.Should().BeNull();

        // The budget really is refreshed: four more failures still do not lock.
        for (var attempt = 1; attempt <= 4; attempt++)
            await LoginAsync(email, WrongPassword);

        var (finalCount, finalLockoutEnd) = await ReadLockoutStateAsync(email);
        finalCount.Should().Be(4);
        finalLockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task LockedAccount_IsRejectedBeforePasswordVerification()
    {
        var email = await RegisterUserAsync();

        for (var attempt = 1; attempt <= 5; attempt++)
            await LoginAsync(email, WrongPassword);

        var (countAfterLock, _) = await ReadLockoutStateAsync(email);
        countAfterLock.Should().Be(5);

        // Further attempts must short-circuit on the lockout check, so the failure
        // counter stops climbing and a locked account cannot be password-probed.
        await LoginAsync(email, WrongPassword);
        await LoginAsync(email, ValidPassword);

        var (countAfterProbes, _) = await ReadLockoutStateAsync(email);
        countAfterProbes.Should().Be(5,
            "requests against a locked account must not reach password verification");
    }

    private sealed class ErrorResponse
    {
        public string? Message { get; set; }
        public string? Code { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }
}
