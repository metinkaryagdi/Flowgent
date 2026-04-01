using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Domain.Enums;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.CompleteSprint;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.CreateSprint;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.StartSprint;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.AddIssue;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.RemoveIssue;
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

    public SprintsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<SprintDto>> Create([FromBody] CreateSprintCommand command)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        var updated = command with { CreatedByUserId = userId };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<SprintDto>> Start(Guid id)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        var command = new StartSprintCommand(id, userId, null);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<SprintDto>> Complete(Guid id, [FromBody] CompleteSprintRequest? request = null)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        var command = new CompleteSprintCommand(
            id,
            userId,
            null,
            request?.CarryOverPolicy ?? SprintCarryOverPolicy.Backlog,
            request?.NextSprintId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("{id:guid}/issues")]
    public async Task<ActionResult<SprintIssueDto>> AddIssue(Guid id, [FromBody] AddIssueCommand command)
    {
        var userId = User.TryGetUserId() ?? Guid.Empty;
        var token = Request.Cookies["accessToken"];
        var updated = command with { SprintId = id, AddedByUserId = userId, BearerToken = string.IsNullOrWhiteSpace(token) ? null : token };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/issues/{issueId:guid}")]
    public async Task<ActionResult<SprintIssueDto>> RemoveIssue(Guid id, Guid issueId, [FromBody] RemoveIssueCommand command)
    {
        var updated = command with { SprintId = id, IssueId = issueId };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpGet("project/{projectId:guid}")]
    public async Task<ActionResult<IReadOnlyList<SprintDto>>> GetByProject(Guid projectId)
    {
        var result = await _mediator.Send(new GetSprintsByProjectQuery(projectId));
        return Ok(result);
    }

    [HttpGet("project/{projectId:guid}/active")]
    public async Task<ActionResult<SprintDto?>> GetActive(Guid projectId)
    {
        var result = await _mediator.Send(new GetActiveSprintQuery(projectId));
        return Ok(result);
    }

    [HttpGet("{id:guid}/issues")]
    public async Task<ActionResult<IReadOnlyList<SprintIssueDto>>> GetIssues(Guid id)
    {
        var result = await _mediator.Send(new GetSprintIssuesQuery(id));
        return Ok(result);
    }

    [HttpGet("{id:guid}/velocity")]
    public async Task<ActionResult<SprintVelocityDto>> GetVelocity(Guid id)
    {
        var result = await _mediator.Send(new GetSprintVelocityQuery(id));
        return Ok(result);
    }

    [HttpGet("project/{projectId:guid}/backlog")]
    public async Task<ActionResult<IReadOnlyList<SprintIssueDto>>> GetBacklog(Guid projectId)
    {
        var result = await _mediator.Send(new GetBacklogQuery(projectId));
        return Ok(result);
    }
}

public sealed record CompleteSprintRequest(
    SprintCarryOverPolicy CarryOverPolicy = SprintCarryOverPolicy.Backlog,
    Guid? NextSprintId = null);
