using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Domain.Enums;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.CompleteSprint;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.CreateSprint;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.StartSprint;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.AddIssue;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.RemoveIssue;
using BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintById;
using BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintIssues;
using BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetBacklog;
using BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetActiveSprint;
using BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintVelocity;
using BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintsByProject;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Extensions;

namespace BitirmeProject.SprintService.Api.Controllers;

[ApiController]
[Route("api/v1/sprints")]
[Authorize]
public sealed class SprintsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISprintRepository _sprintRepository;

    public SprintsController(IMediator mediator, ISprintRepository sprintRepository)
    {
        _mediator = mediator;
        _sprintRepository = sprintRepository;
    }

    [HttpPost]
    public async Task<ActionResult<SprintDto>> Create([FromBody] CreateSprintCommand command)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
            return Unauthorized();

        var orgId = User.TryGetOrganizationId();
        var updated = command with { CreatedByUserId = userId.Value, OrganizationId = orgId };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SprintDto>> GetById(Guid id)
    {
        var (_, error) = await AuthorizeSprintAccessAsync(id);
        if (error is not null)
            return error;

        var result = await _mediator.Send(new GetSprintByIdQuery(id));
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<SprintDto>> Start(Guid id)
    {
        var (sprint, error) = await AuthorizeSprintAccessAsync(id);
        if (error is not null)
            return error;

        var userId = User.TryGetUserId();
        if (userId is null)
            return Unauthorized();

        var command = new StartSprintCommand(id, userId.Value, null);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<SprintDto>> Complete(Guid id, [FromBody] CompleteSprintRequest? request = null)
    {
        var (sprint, error) = await AuthorizeSprintAccessAsync(id);
        if (error is not null)
            return error;

        var userId = User.TryGetUserId();
        if (userId is null)
            return Unauthorized();

        var command = new CompleteSprintCommand(
            id,
            userId.Value,
            null,
            request?.CarryOverPolicy ?? SprintCarryOverPolicy.Backlog,
            request?.NextSprintId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("{id:guid}/issues")]
    public async Task<ActionResult<SprintIssueDto>> AddIssue(Guid id, [FromBody] AddIssueCommand command)
    {
        var (_, error) = await AuthorizeSprintAccessAsync(id);
        if (error is not null)
            return error;

        var userId = User.TryGetUserId();
        if (userId is null)
            return Unauthorized();

        var token = Request.Cookies["accessToken"];
        var updated = command with { SprintId = id, AddedByUserId = userId.Value, BearerToken = string.IsNullOrWhiteSpace(token) ? null : token };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/issues/{issueId:guid}")]
    public async Task<ActionResult<SprintIssueDto>> RemoveIssue(Guid id, Guid issueId)
    {
        var (_, error) = await AuthorizeSprintAccessAsync(id);
        if (error is not null)
            return error;

        var userId = User.TryGetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new RemoveIssueCommand(id, issueId, userId.Value, null));
        return Ok(result);
    }

    [HttpGet("project/{projectId:guid}")]
    public async Task<ActionResult<IReadOnlyList<SprintDto>>> GetByProject(Guid projectId)
    {
        Guid? callerOrgId = Guid.TryParse(Request.Headers["X-Organization-Id"].FirstOrDefault()
            ?? User.FindFirst("org_id")?.Value, out var g) ? g : null;
        var result = await _mediator.Send(new GetSprintsByProjectQuery(projectId, callerOrgId));
        return Ok(result);
    }

    [HttpGet("project/{projectId:guid}/active")]
    public async Task<ActionResult<SprintDto?>> GetActive(Guid projectId)
    {
        Guid? callerOrgId = Guid.TryParse(Request.Headers["X-Organization-Id"].FirstOrDefault()
            ?? User.FindFirst("org_id")?.Value, out var g) ? g : null;
        var result = await _mediator.Send(new GetActiveSprintQuery(projectId, callerOrgId));
        return Ok(result);
    }

    [HttpGet("{id:guid}/issues")]
    public async Task<ActionResult<IReadOnlyList<SprintIssueDto>>> GetIssues(Guid id)
    {
        var (_, error) = await AuthorizeSprintAccessAsync(id);
        if (error is not null)
            return error;

        var result = await _mediator.Send(new GetSprintIssuesQuery(id));
        return Ok(result);
    }

    [HttpGet("{id:guid}/velocity")]
    public async Task<ActionResult<SprintVelocityDto>> GetVelocity(Guid id)
    {
        var (_, error) = await AuthorizeSprintAccessAsync(id);
        if (error is not null)
            return error;

        var result = await _mediator.Send(new GetSprintVelocityQuery(id));
        return Ok(result);
    }

    [HttpGet("project/{projectId:guid}/backlog")]
    public async Task<ActionResult<IReadOnlyList<SprintIssueDto>>> GetBacklog(Guid projectId)
    {
        var result = await _mediator.Send(new GetBacklogQuery(projectId));
        return Ok(result);
    }

    private async Task<(Domain.Entities.Sprint? Sprint, ActionResult? Error)> AuthorizeSprintAccessAsync(Guid sprintId)
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var sprint = await _sprintRepository.GetByIdAsync(sprintId, cancellationToken);
        if (sprint is null)
            return (null, NotFound());

        if (User.HasRole("Admin") || User.IsInternalCall())
            return (sprint, null);

        var callerOrgId = User.TryGetOrganizationId();
        if (sprint.OrganizationId.HasValue && sprint.OrganizationId != callerOrgId)
            return (null, Forbid());

        return (sprint, null);
    }
}

public sealed record CompleteSprintRequest(
    SprintCarryOverPolicy CarryOverPolicy = SprintCarryOverPolicy.Backlog,
    Guid? NextSprintId = null);
