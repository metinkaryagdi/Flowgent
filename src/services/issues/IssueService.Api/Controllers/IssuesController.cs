using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.AssignIssue;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.ChangeIssueStatus;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.CreateIssue;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeProject.IssueService.Api.Controllers;

[ApiController]
[Route("api/v1/issues")]
[Authorize]
public sealed class IssuesController : ControllerBase
{
    private readonly IMediator _mediator;

    public IssuesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<IssueDto>> Create([FromBody] CreateIssueCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IssueDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetIssueByIdQuery(id));
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<IssueDto>> Assign(Guid id, [FromBody] AssignIssueCommand command)
    {
        var updated = command with { IssueId = id };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<IssueDto>> ChangeStatus(Guid id, [FromBody] ChangeIssueStatusCommand command)
    {
        var updated = command with { IssueId = id };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }
}

