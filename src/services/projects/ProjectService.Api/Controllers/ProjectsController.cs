using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.DeleteProject;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.AddMember;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.RemoveMember;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.UpdateProject;
using BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectById;
using BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectsByUser;
using BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetTeamMembers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeProject.ProjectService.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public sealed class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(id));
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetByUser(Guid userId)
    {
        var result = await _mediator.Send(new GetProjectsByUserQuery(userId));
        return Ok(result);
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<ProjectMemberDto>>> GetMembers(Guid id)
    {
        var result = await _mediator.Send(new GetTeamMembersQuery(id));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, [FromBody] UpdateProjectCommand command)
    {
        var updated = command with { Id = id };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteProjectCommand(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<ProjectDto>> AddMember(Guid id, [FromBody] AddMemberCommand command)
    {
        var updated = command with { ProjectId = id };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<ActionResult<ProjectDto>> RemoveMember(Guid id, Guid userId)
    {
        var result = await _mediator.Send(new RemoveMemberCommand(id, userId));
        return Ok(result);
    }
}

