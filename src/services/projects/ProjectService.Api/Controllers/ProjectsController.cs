using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.DeleteProject;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.AddMember;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.RemoveMember;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.UpdateProject;
using BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectById;
using BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectsByUser;
using BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectsByUserPaged;
using BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetTeamMembers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Extensions;

namespace BitirmeProject.ProjectService.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public sealed class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IProjectRepository _projectRepository;

    public ProjectsController(IMediator mediator, IProjectRepository projectRepository)
    {
        _mediator = mediator;
        _projectRepository = projectRepository;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectCommand command)
    {
        // OwnerUserId must come from authenticated Claims, never from the request body.
        var ownerUserId = User.GetUserId();
        var safeCommand = command with { OwnerUserId = ownerUserId };
        var result = await _mediator.Send(safeCommand);
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
        // Only the user themselves or an Admin may query user-specific projects.
        var requesterId = User.TryGetUserId();
        if (!User.HasRole("Admin") && requesterId != userId)
            return Forbid();

        var result = await _mediator.Send(new GetProjectsByUserQuery(userId));
        return Ok(result);
    }

    [HttpGet("user/{userId:guid}/paged")]
    public async Task<ActionResult<PagedResult<ProjectDto>>> GetByUserPaged(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? search = null,
        [FromQuery] bool includeArchived = false)
    {
        // Only the user themselves or an Admin may query user-specific projects.
        var requesterId = User.TryGetUserId();
        if (!User.HasRole("Admin") && requesterId != userId)
            return Forbid();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;
        if (pageSize > 200) pageSize = 200;

        var result = await _mediator.Send(new GetProjectsByUserPagedQuery(userId, page, pageSize, search, includeArchived));
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
        // Ownership guard: only the project owner or an Admin may update.
        var callerId = User.GetUserId();
        var isAdmin = User.HasRole("Admin");
        if (!isAdmin)
        {
            var project = await _projectRepository.GetByIdAsync(id, HttpContext.RequestAborted);
            if (project is null) return NotFound();
            if (project.OwnerUserId != callerId) return Forbid();
        }

        // UpdatedByUserId must come from authenticated Claims, never from the request body.
        var updated = command with { Id = id, UpdatedByUserId = callerId };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Delete(Guid id)
    {
        // Ownership guard: only the project owner or an Admin may delete.
        var callerId = User.GetUserId();
        var isAdmin = User.HasRole("Admin");
        if (!isAdmin)
        {
            var project = await _projectRepository.GetByIdAsync(id, HttpContext.RequestAborted);
            if (project is null) return NotFound();
            if (project.OwnerUserId != callerId) return Forbid();
        }

        var result = await _mediator.Send(new DeleteProjectCommand(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<ProjectDto>> AddMember(Guid id, [FromBody] AddMemberCommand command)
    {
        var updated = command with
        {
            ProjectId = id,
            AddedByUserId = User.GetUserId()
        };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<ActionResult<ProjectDto>> RemoveMember(Guid id, Guid userId)
    {
        var result = await _mediator.Send(new RemoveMemberCommand(id, userId, User.GetUserId()));
        return Ok(result);
    }
}

