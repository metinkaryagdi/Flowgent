using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.RegisterUser;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.UpdateUser;
using BitirmeProject.IdentityService.Application.Features.Users.Queries.GetUserById;
using IdentityService.Application.Features.Users.Commands.DeleteUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeProject.IdentityService.Api.Controllers;

[ApiController]
[Route("api/v1/identity/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterUserCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        if (result is null)
            return NotFound();

        return Ok(result);
    }
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserCommand command)
    {
        var updatedCommand = command with { Id = id };
        var result = await _mediator.Send(updatedCommand);
        if (result is null)
            return NotFound();
        return Ok(result);
    }
    //[Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var command = new DeleteUserCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }

}


