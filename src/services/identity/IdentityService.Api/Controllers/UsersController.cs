using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.RegisterUser;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.UpdateUser;
using BitirmeProject.IdentityService.Application.Features.Users.Queries.GetUserById;
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

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
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
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var command = new DeleteUserCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}



