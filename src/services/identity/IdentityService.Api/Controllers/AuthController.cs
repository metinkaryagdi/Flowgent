using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Login;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Refresh;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Revoke;
using BitirmeProject.IdentityService.Application.Options;
using System.IdentityModel.Tokens.Jwt;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BitirmeProject.IdentityService.Api.Controllers;

[ApiController]
[Route("api/v1/identity")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly JwtOptions _jwtOptions;

    public AuthController(IMediator mediator, IWebHostEnvironment env, IOptions<JwtOptions> jwtOptions)
    {
        _mediator = mediator;
        _env = env;
        _jwtOptions = jwtOptions.Value;
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

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized("Refresh token cookie is missing.");

        var result = await _mediator.Send(
            new RefreshTokenCommand(refreshToken, TryGetOrganizationIdFromAccessToken()),
            cancellationToken);
        SetTokenCookies(result);
        return Ok(StripTokens(result));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrWhiteSpace(refreshToken))
            await _mediator.Send(new RevokeTokenCommand(refreshToken), cancellationToken);

        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    // ── Helper: JWT + Refresh token'ları HttpOnly cookie olarak ekle ──
    private void SetTokenCookies(AuthResponseDto result)
    {
        // Secure=true only in Production (requires HTTPS).
        // In Development/Testing docker-compose runs on plain HTTP.
        var isSecure = _env.IsProduction();

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Lax,
            Expires = result.ExpiresAt,
            Path = "/"
        };

        Response.Cookies.Append("accessToken", result.AccessToken, cookieOptions);

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
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
        Roles = result.Roles,
        ActiveOrgId = result.ActiveOrgId,
        ActiveOrgName = result.ActiveOrgName,
        ActiveOrgRole = result.ActiveOrgRole,
    };

    private Guid? TryGetOrganizationIdFromAccessToken()
    {
        var accessToken = Request.Cookies["accessToken"];
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        try
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var orgIdClaim = token.Claims.FirstOrDefault(c => c.Type == "org_id")?.Value;
            return Guid.TryParse(orgIdClaim, out var organizationId) ? organizationId : null;
        }
        catch
        {
            return null;
        }
    }
}

