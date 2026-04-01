using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Organizations.Commands.ChangeMemberRole;
using BitirmeProject.IdentityService.Application.Features.Organizations.Commands.CreateOrganization;
using BitirmeProject.IdentityService.Application.Features.Organizations.Commands.RemoveMember;
using BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetMyOrganization;
using BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetOrganizationMembers;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Extensions;

namespace BitirmeProject.IdentityService.Api.Controllers;

[ApiController]
[Route("api/v1/identity/organizations")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new organization. The requesting user becomes the Owner.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> Create([FromBody] CreateOrganizationRequest request)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new CreateOrganizationCommand(request.Name, userId));
        return CreatedAtAction(nameof(GetMy), result);
    }

    /// <summary>
    /// Returns the organization the requesting user belongs to.
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<OrganizationDto>> GetMy()
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new GetMyOrganizationQuery(userId));
        if (result is null) return NotFound();

        return Ok(result);
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

        await _mediator.Send(new RemoveMemberCommand(id, userId, targetUserId));
        return NoContent();
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

        await _mediator.Send(new ChangeMemberRoleCommand(id, userId, targetUserId, request.Role));
        return NoContent();
    }
}

public sealed record CreateOrganizationRequest(string Name);
public sealed record ChangeMemberRoleRequest(OrganizationRole Role);
