using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Login;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeProject.IdentityService.Api.Controllers;

[ApiController]
[Route("api/v1/identity")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        SetTokenCookies(result);
        return Ok(StripTokens(result));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        SetTokenCookies(result);
        return Ok(StripTokens(result));
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    // ── Helper: JWT + Refresh token'ları HttpOnly cookie olarak ekle ──
    private void SetTokenCookies(AuthResponseDto result)
    {
        // Always use Secure=true — the app runs behind a reverse proxy that handles HTTPS.
        // Using Request.IsHttps here would incorrectly return false in many proxy setups.
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = result.ExpiresAt,
            Path = "/"
        };

        Response.Cookies.Append("accessToken", result.AccessToken, cookieOptions);

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Path = "/api/v1/identity"
        };

        Response.Cookies.Append("refreshToken", result.RefreshToken, refreshCookieOptions);
    }

    // ── Helper: JSON body'den token değerlerini sil ──
    private static AuthResponseDto StripTokens(AuthResponseDto result) => new()
    {
        AccessToken = string.Empty,
        RefreshToken = string.Empty,
        ExpiresAt = result.ExpiresAt,
        User = result.User,
        Roles = result.Roles
    };
}

