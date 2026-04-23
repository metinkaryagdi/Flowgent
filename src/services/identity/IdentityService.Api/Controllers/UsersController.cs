using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Common;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.AssignRoleToUser;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.RegisterUser;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.UpdateUser;
using BitirmeProject.IdentityService.Application.Features.Users.Queries.GetUserById;
using BitirmeProject.IdentityService.Domain.Enums;
using IdentityService.Application.Features.Users.Commands.DeleteUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Extensions;

namespace BitirmeProject.IdentityService.Api.Controllers;

[ApiController]
[Route("api/v1/identity/users")]
[Authorize] // All endpoints require authentication by default
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRefreshTokenRepository? _refreshTokenRepository;

    public UsersController(
        IMediator mediator,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork,
        IRefreshTokenRepository? refreshTokenRepository = null)
    {
        _mediator = mediator;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
        _refreshTokenRepository = refreshTokenRepository;
    }

    /// <summary>
    /// Admin-only: creates a managed user account (not self-registration).
    /// Self-registration is via POST /api/v1/identity/register.
    /// </summary>
    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterUserCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Returns a user by ID. Accessible by: the user themselves or an Admin.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var requesterId = User.TryGetUserId();
        var isAdmin = User.HasRole("Admin");

        // Only allow self-lookup or admin access
        if (!isAdmin && requesterId != id)
            return Forbid();

        var result = await _mediator.Send(new GetUserByIdQuery(id));
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Updates a user. Only the user themselves or an Admin may update.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserCommand command)
    {
        var requesterId = User.TryGetUserId();
        var isAdmin = User.HasRole("Admin");

        if (!isAdmin && requesterId != id)
            return Forbid();

        var updatedCommand = command with { Id = id };
        var result = await _mediator.Send(updatedCommand);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Admin-only: soft deletes a user.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken = default)
    {
        if (User.TryGetUserId() == id)
            return BadRequest("You cannot delete your own account from the admin panel.");

        var targetUser = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (targetUser is not null && IsAdmin(targetUser) && targetUser.IsActive
            && !await HasAnotherActiveAdminAsync(id, cancellationToken))
            return BadRequest("At least one active admin must remain.");

        var command = new DeleteUserCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: lists all users with their organization name.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var orgs = await _organizationRepository.GetAllAsync(cancellationToken);

        // Build userId → orgName map
        var userOrgMap = new Dictionary<Guid, string>();
        foreach (var org in orgs)
            foreach (var member in org.Members)
                userOrgMap.TryAdd(member.UserId, org.Name);

        var dtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            OrgName = userOrgMap.GetValueOrDefault(u.Id),
        }).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Admin-only: returns high-level system statistics.
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AdminStatsDto>> GetStats(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var orgs = await _organizationRepository.GetAllAsync(cancellationToken);

        return Ok(new AdminStatsDto
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            TotalOrgs = orgs.Count,
        });
    }

    /// <summary>
    /// Admin-only: returns the role names for a user.
    /// </summary>
    [HttpGet("{id:guid}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetUserRoles(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return NotFound();
        var roleNames = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        return Ok(roleNames);
    }

    /// <summary>
    /// Admin-only: assigns a role to a user by role name.
    /// </summary>
    [HttpPost("{id:guid}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByNameAsync(request.RoleName, cancellationToken);
        if (role is null) return NotFound($"Role '{request.RoleName}' not found.");
        await _mediator.Send(new AssignRoleToUserCommand(id, role.Id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: removes a role from a user by role name.
    /// </summary>
    [HttpDelete("{id:guid}/roles/{roleName}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveRole(Guid id, string roleName, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return NotFound();
        var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
        if (role is null) return NotFound($"Role '{roleName}' not found.");

        if (role.Name.Equals(DefaultIdentityRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            if (User.TryGetUserId() == id)
                return BadRequest("You cannot remove your own admin role.");

            if (user.IsActive && !await HasAnotherActiveAdminAsync(id, cancellationToken))
                return BadRequest("At least one active admin must remain.");
        }

        user.RemoveRole(role.Id);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: deactivates a user.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        if (User.TryGetUserId() == id)
            return BadRequest("You cannot deactivate your own account.");

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return NotFound();

        if (IsAdmin(user) && user.IsActive && !await HasAnotherActiveAdminAsync(id, cancellationToken))
            return BadRequest("At least one active admin must remain.");

        user.ChangeStatus(UserStatus.Deactivated);

        if (_refreshTokenRepository is not null)
        {
            var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(id, cancellationToken);
            foreach (var token in activeTokens)
                token.Revoke();
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: activates a user.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return NotFound();
        user.ChangeStatus(UserStatus.Active);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasAnotherActiveAdminAsync(Guid excludedUserId, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Any(user =>
            user.Id != excludedUserId
            && user.IsActive
            && IsAdmin(user));
    }

    private static bool IsAdmin(global::User user)
    {
        return user.UserRoles.Any(userRole =>
            userRole.Role?.Name.Equals(DefaultIdentityRoles.Admin, StringComparison.OrdinalIgnoreCase) == true);
    }
}

public sealed record AssignRoleRequest(string RoleName);

public sealed record AdminStatsDto
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int TotalOrgs { get; init; }
}



