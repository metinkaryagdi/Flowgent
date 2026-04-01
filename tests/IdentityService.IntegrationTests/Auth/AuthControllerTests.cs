using Xunit;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IdentityService.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using BitirmeProject.IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.IntegrationTests.Auth;

public sealed class AuthControllerTests : IClassFixture<IdentityWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly IdentityWebAppFactory _factory;

    public AuthControllerTests(IdentityWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ─── Register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_Returns200AndUserInfo()
    {
        var request = new
        {
            UserName = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "Test1234!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/identity/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.User.Should().NotBeNull();
        body.User.Email.Should().Be(request.Email);
        body.AccessToken.Should().BeEmpty(); // cookie'ye taşındı, body boş olmalı
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var email = $"dup_{Guid.NewGuid():N}@example.com";
        var request = new
        {
            UserName = $"user1_{Guid.NewGuid():N}",
            Email = email,
            Password = "Test1234!"
        };

        // İlk kayıt başarılı
        await _client.PostAsJsonAsync("/api/v1/identity/register", request);

        // İkinci kayıt aynı email → hata
        var request2 = new
        {
            UserName = $"user2_{Guid.NewGuid():N}",
            Email = email,
            Password = "Test1234!"
        };
        var response = await _client.PostAsJsonAsync("/api/v1/identity/register", request2);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        var request = new
        {
            UserName = $"user_{Guid.NewGuid():N}",
            Email = $"short_{Guid.NewGuid():N}@example.com",
            Password = "abc" // çok kısa
        };

        var response = await _client.PostAsJsonAsync("/api/v1/identity/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── Login ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndSetsCookies()
    {
        // Önce kayıt ol
        var email = $"login_{Guid.NewGuid():N}@example.com";
        var password = "Test1234!";
        await _client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            UserName = $"loginuser_{Guid.NewGuid():N}",
            Email = email,
            Password = password
        });

        // Giriş yap
        var response = await _client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            UserNameOrEmail = email,
            Password = password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.User.Email.Should().Be(email);

        // accessToken cookie set-cookie header'ında olmalı
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        cookies.Should().NotBeNull();
        cookies!.Any(c => c.StartsWith("accessToken=")).Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = $"wrongpass_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            UserName = $"wrongpass_{Guid.NewGuid():N}",
            Email = email,
            Password = "Correct1234!"
        });

        var response = await _client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            UserNameOrEmail = email,
            Password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            UserNameOrEmail = "nouser@example.com",
            Password = "Test1234!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Logout ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/identity/logout", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Refresh ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/identity/refresh", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Response DTOs ───────────────────────────────────────────────────────

    private sealed class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserResponse User { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
    }

    private sealed class UserResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
