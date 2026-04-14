using System.Security.Claims;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Organizations.Commands.ChangeMemberRole;
using BitirmeProject.IdentityService.Application.Features.Organizations.Commands.CreateOrganization;
using BitirmeProject.IdentityService.Application.Features.Organizations.Commands.RemoveMember;
using BitirmeProject.IdentityService.Application.Features.Organizations.Commands.SwitchOrganization;
using BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetMyOrganization;
using BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetMyOrganizations;
using BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetOrganizationMembers;
using BitirmeProject.IdentityService.Application.Options;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Common.Extensions;

namespace BitirmeProject.IdentityService.Api.Controllers;

[ApiController]
[Route("api/v1/identity/organizations")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly JwtOptions _jwtOptions;
    private readonly IOrganizationRepository _organizationRepository;

    public OrganizationsController(IMediator mediator, IWebHostEnvironment env, IOptions<JwtOptions> jwtOptions, IOrganizationRepository organizationRepository)
    {
        _mediator = mediator;
        _env = env;
        _jwtOptions = jwtOptions.Value;
        _organizationRepository = organizationRepository;
    }

    /// <summary>
    /// Creates a new organization. The requesting user becomes the Owner.
    /// Issues a new access token with the created org as the active context.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SwitchOrganizationResponse>> Create([FromBody] CreateOrganizationRequest request)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        var org = await _mediator.Send(new CreateOrganizationCommand(request.Name, userId));

        // Issue a new JWT with the newly created org as context
        var switchResult = await _mediator.Send(new SwitchOrganizationCommand(userId, org.Id));

        var isSecure = _env.IsProduction();
        Response.Cookies.Append("accessToken", switchResult.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Lax,
            Expires = switchResult.ExpiresAt,
            Path = "/"
        });

        return CreatedAtAction(nameof(GetMy), new SwitchOrganizationResponse(org.Id.ToString(), org.Name, switchResult.OrgRole));
    }

    /// <summary>
    /// Returns the currently active organization (from JWT org_id claim, or first org).
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<OrganizationDto>> GetMy()
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        Guid? orgId = null;
        var orgIdClaim = User.FindFirstValue("org_id");
        if (!string.IsNullOrWhiteSpace(orgIdClaim) && Guid.TryParse(orgIdClaim, out var parsedOrgId))
            orgId = parsedOrgId;

        var result = await _mediator.Send(new GetMyOrganizationQuery(userId, orgId));
        if (result is null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Returns all organizations the requesting user belongs to.
    /// </summary>
    [HttpGet("my/all")]
    public async Task<ActionResult<List<OrganizationDto>>> GetAll()
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new GetMyOrganizationsQuery(userId));
        return Ok(result);
    }

    /// <summary>
    /// Switches the active organization. Issues a new JWT with the selected org context.
    /// </summary>
    [HttpPost("{id:guid}/switch")]
    public async Task<ActionResult<SwitchOrganizationResponse>> Switch(Guid id)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new SwitchOrganizationCommand(userId, id));

        // Issue new access token cookie
        var isSecure = _env.IsProduction();
        Response.Cookies.Append("accessToken", result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Lax,
            Expires = result.ExpiresAt,
            Path = "/"
        });

        return Ok(new SwitchOrganizationResponse(id.ToString(), result.OrgName, result.OrgRole));
    }

    /// <summary>
    /// Lists all members of the given organization.
    /// </summary>
    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<OrganizationMemberDto>>> GetMembers(Guid id)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new GetOrganizationMembersQuery(id, userId));
        return Ok(result);
    }

    /// <summary>
    /// Removes a member from the organization. Owner or Manager only.
    /// </summary>
    [HttpDelete("{id:guid}/members/{targetUserId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid targetUserId)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        // Fast-fail: Member role cannot remove members
        var orgRole = User.FindFirstValue("org_role");
        if (orgRole == "Member") return Forbid();

        // Cross-org guard: requested org must match active JWT org
        var jwtOrgId = User.FindFirstValue("org_id");
        if (!Guid.TryParse(jwtOrgId, out var activeOrgId) || activeOrgId != id)
            return Forbid();

        await _mediator.Send(new RemoveMemberCommand(id, userId, targetUserId));
        return NoContent();
    }

    /// <summary>
    /// Admin-only: lists all organizations in the system.
    /// </summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<OrganizationDto>>> GetAllForAdmin(CancellationToken cancellationToken)
    {
        var orgs = await _organizationRepository.GetAllAsync(cancellationToken);
        var dtos = orgs.Select(o => new OrganizationDto
        {
            Id = o.Id,
            Name = o.Name,
            CreatedByUserId = o.CreatedByUserId,
            MemberCount = o.Members.Count,
            CreatedAt = o.CreatedAt,
        }).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Changes a member's role. Owner only.
    /// </summary>
    [HttpPut("{id:guid}/members/{targetUserId:guid}/role")]
    public async Task<IActionResult> ChangeMemberRole(
        Guid id,
        Guid targetUserId,
        [FromBody] ChangeMemberRoleRequest request)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        // Fast-fail: only Owner can change roles
        var orgRole = User.FindFirstValue("org_role");
        if (orgRole != "Owner") return Forbid();

        // Cross-org guard
        var jwtOrgId = User.FindFirstValue("org_id");
        if (!Guid.TryParse(jwtOrgId, out var activeOrgId) || activeOrgId != id)
            return Forbid();

        var role = request.Role ?? request.NewRole;
        if (!role.HasValue)
            return BadRequest("Role is required.");

        await _mediator.Send(new ChangeMemberRoleCommand(id, userId, targetUserId, role.Value));
        return NoContent();
    }
}

public sealed record CreateOrganizationRequest(string Name);
public sealed class ChangeMemberRoleRequest
{
    public OrganizationRole? Role { get; init; }
    public OrganizationRole? NewRole { get; init; }
}
public sealed record SwitchOrganizationResponse(string OrgId, string OrgName, string OrgRole);
