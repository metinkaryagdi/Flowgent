using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Invites.Commands.AcceptInvite;
using BitirmeProject.IdentityService.Application.Features.Invites.Commands.AcceptInviteExisting;
using BitirmeProject.IdentityService.Application.Features.Invites.Commands.RevokeInvite;
using BitirmeProject.IdentityService.Application.Features.Invites.Commands.SendInvite;
using BitirmeProject.IdentityService.Application.Features.Invites.Queries.GetPendingInvites;
using BitirmeProject.IdentityService.Application.Features.Invites.Queries.ValidateInviteToken;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Extensions;

namespace BitirmeProject.IdentityService.Api.Controllers;

[ApiController]
[Route("api/v1/identity/invites")]
public class InvitesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public InvitesController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    /// <summary>
    /// Sends an invite email to the given address. Owner or Manager only.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<InviteDto>> SendInvite([FromBody] SendInviteRequest request)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        // Fast-fail: Member role cannot send invites
        var orgRole = User.FindFirst("org_role")?.Value;
        if (orgRole == "Member") return Forbid();

        // Cross-org guard: can only invite to the active org in JWT
        var jwtOrgId = User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(jwtOrgId, out var activeOrgId) || activeOrgId != request.OrganizationId)
            return Forbid();

        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5173";

        var result = await _mediator.Send(new SendInviteCommand(
            request.OrganizationId,
            userId,
            request.Email,
            request.Role,
            baseUrl));

        return Ok(result);
    }

    /// <summary>
    /// Validates an invite token. Anonymous — used by the frontend before showing the registration form.
    /// </summary>
    [HttpGet("validate/{token:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ValidateInviteTokenResult>> Validate(Guid token)
    {
        var result = await _mediator.Send(new ValidateInviteTokenQuery(token));
        if (result is null) return NotFound(new { message = "Invite token is invalid or expired." });

        return Ok(result);
    }

    /// <summary>
    /// Accepts an invite: creates the user account and adds them to the organization.
    /// </summary>
    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Accept([FromBody] AcceptInviteRequest request)
    {
        var result = await _mediator.Send(new AcceptInviteCommand(
            request.Token,
            request.UserName,
            request.Password));

        return Ok(result);
    }

    /// <summary>
    /// Accepts an invite for an already-registered user. Requires authentication.
    /// </summary>
    [HttpPost("accept-existing")]
    [Authorize]
    public async Task<ActionResult<UserDto>> AcceptAsExisting([FromBody] AcceptAsExistingRequest request)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new AcceptInviteExistingCommand(request.Token, userId));
        return Ok(result);
    }

    /// <summary>
    /// Lists pending (not yet accepted/expired) invites for the organization.
    /// </summary>
    [HttpGet("pending")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<InviteDto>>> GetPending([FromQuery] Guid organizationId)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        // Fast-fail: Member role cannot view pending invites
        var orgRole = User.FindFirst("org_role")?.Value;
        if (orgRole == "Member") return Forbid();

        // Cross-org guard: can only view invites for the active org in JWT
        var jwtOrgId = User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(jwtOrgId, out var activeOrgId) || activeOrgId != organizationId)
            return Forbid();

        var result = await _mediator.Send(new GetPendingInvitesQuery(organizationId, userId));
        return Ok(result);
    }

    /// <summary>
    /// Revokes a pending invite. Owner or Manager only.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        // Fast-fail: Member role cannot revoke invites
        var orgRole = User.FindFirst("org_role")?.Value;
        if (orgRole == "Member") return Forbid();

        await _mediator.Send(new RevokeInviteCommand(id, userId));
        return NoContent();
    }
}

public sealed record SendInviteRequest(Guid OrganizationId, string Email, OrganizationRole Role);
public sealed record AcceptInviteRequest(Guid Token, string UserName, string Password);
public sealed record AcceptAsExistingRequest(Guid Token);
