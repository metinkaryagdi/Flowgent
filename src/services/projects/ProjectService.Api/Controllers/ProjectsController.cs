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
using BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectsByOrganizationPaged;
using BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetAllProjectsPaged;
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
    private readonly IUnitOfWork _unitOfWork;

    public ProjectsController(IMediator mediator, IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectCommand command)
    {
        var ownerUserId = User.GetUserId();
        var orgId = User.TryGetOrganizationId();
        var safeCommand = command with { OwnerUserId = ownerUserId, OrganizationId = orgId };
        var result = await _mediator.Send(safeCommand);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Returns all projects belonging to the caller's active organization (from JWT org_id claim), paged.
    /// This is the primary endpoint for org-member project listing.
    /// </summary>
    [HttpGet("organization/paged")]
    public async Task<ActionResult<PagedResult<ProjectDto>>> GetByOrganizationPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? search = null,
        [FromQuery] bool includeArchived = false)
    {
        var orgId = User.TryGetOrganizationId();
        if (!orgId.HasValue)
            return BadRequest("No active organization context. Please switch to an organization first.");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;
        if (pageSize > 200) pageSize = 200;

        var result = await _mediator.Send(new GetProjectsByOrganizationPagedQuery(
            orgId.Value, page, pageSize, search, includeArchived));
        return Ok(result);
    }

    /// <summary>
    /// Admin-only: returns all projects across all organizations, paged.
    /// </summary>
    [HttpGet("admin/paged")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<ProjectDto>>> GetAllPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? search = null,
        [FromQuery] bool includeArchived = false)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;
        if (pageSize > 200) pageSize = 200;

        var result = await _mediator.Send(new GetAllProjectsPagedQuery(page, pageSize, search, includeArchived));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id)
    {
        var (_, error) = await AuthorizeProjectScopeAsync(id);
        if (error is not null)
            return error;

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

        var orgId = User.TryGetOrganizationId();
        var result = await _mediator.Send(new GetProjectsByUserQuery(userId, orgId));
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

        var result = await _mediator.Send(new GetProjectsByUserPagedQuery(
            userId,
            User.TryGetOrganizationId(),
            page,
            pageSize,
            search,
            includeArchived));
        return Ok(result);
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<ProjectMemberDto>>> GetMembers(Guid id)
    {
        var (_, error) = await AuthorizeProjectScopeAsync(id);
        if (error is not null)
            return error;

        var result = await _mediator.Send(new GetTeamMembersQuery(id));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, [FromBody] UpdateProjectCommand command)
    {
        // Ownership guard: project owner, org Manager/Owner, or system Admin may update.
        var callerId = User.GetUserId();
        var isAdmin = User.HasRole("Admin");
        var (project, error) = await AuthorizeProjectScopeAsync(id);
        if (error is not null)
            return error;

        if (!isAdmin && project is not null)
        {
            var orgRole = User.GetOrganizationRole();
            var isOrgManager = orgRole is "Owner" or "Manager";
            if (project.OwnerUserId != callerId && !isOrgManager) return Forbid();
        }

        // UpdatedByUserId must come from authenticated Claims, never from the request body.
        var updated = command with { Id = id, UpdatedByUserId = callerId };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Delete(Guid id)
    {
        // Ownership guard: project owner, org Manager/Owner, or system Admin may delete.
        var callerId = User.GetUserId();
        var isAdmin = User.HasRole("Admin");
        var (project, error) = await AuthorizeProjectScopeAsync(id);
        if (error is not null)
            return error;

        if (!isAdmin && project is not null)
        {
            var orgRole = User.GetOrganizationRole();
            var isOrgManager = orgRole is "Owner" or "Manager";
            if (project.OwnerUserId != callerId && !isOrgManager) return Forbid();
        }

        var result = await _mediator.Send(new DeleteProjectCommand(id));
        return Ok(result);
    }

    [HttpDelete("admin/{id:guid}/hard-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HardDelete(Guid id)
    {
        var project = await _projectRepository.GetByIdAsync(id, HttpContext.RequestAborted);
        if (project is null)
            return NotFound();

        await _projectRepository.DeleteAsync(project, HttpContext.RequestAborted);
        await _unitOfWork.SaveChangesAsync(HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<ProjectDto>> AddMember(Guid id, [FromBody] AddMemberCommand command)
    {
        var (_, error) = await AuthorizeProjectScopeAsync(id);
        if (error is not null)
            return error;

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
        var (_, error) = await AuthorizeProjectScopeAsync(id);
        if (error is not null)
            return error;

        var result = await _mediator.Send(new RemoveMemberCommand(id, userId, User.GetUserId()));
        return Ok(result);
    }

    private async Task<(Domain.Entities.Project? Project, ActionResult? Error)> AuthorizeProjectScopeAsync(Guid projectId)
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
            return (null, NotFound());

        if (User.HasRole("Admin"))
            return (project, null);

        var callerOrgId = User.TryGetOrganizationId();
        if (project.OrganizationId.HasValue && project.OrganizationId != callerOrgId)
            return (null, Forbid());

        return (project, null);
    }
}
