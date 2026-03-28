using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.AssignIssue;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.AddComment;
using System.Security.Claims;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.AttachFile;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.ChangeIssueStatus;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.CreateIssue;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueAttachments;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueHistory;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueById;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByAssignee;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProject;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProjectPaged;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesBySprint;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueWorkflowConfig;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Abstractions.Exceptions;

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

    [HttpGet("project/{projectId:guid}")]
    public async Task<ActionResult<IReadOnlyList<IssueBoardItemDto>>> GetByProject(Guid projectId)
    {
        var result = await _mediator.Send(new GetIssuesByProjectQuery(projectId));
        return Ok(result);
    }

    [HttpGet("project/{projectId:guid}/paged")]
    public async Task<ActionResult<PagedResult<IssueBoardItemDto>>> GetByProjectPaged(
        Guid projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? sprintId = null,
        [FromQuery] bool backlogOnly = false)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var result = await _mediator.Send(new GetIssuesByProjectPagedQuery(projectId, page, pageSize, sprintId, backlogOnly));
        return Ok(result);
    }

    [HttpGet("assignee/{assigneeUserId:guid}")]
    public async Task<ActionResult<IReadOnlyList<IssueDto>>> GetByAssignee(Guid assigneeUserId)
    {
        var result = await _mediator.Send(new GetIssuesByAssigneeQuery(assigneeUserId));
        return Ok(result);
    }

    [HttpGet("sprint/{sprintId:guid}")]
    public async Task<ActionResult<IReadOnlyList<IssueDto>>> GetBySprint(Guid sprintId)
    {
        var result = await _mediator.Send(new GetIssuesBySprintQuery(sprintId));
        return Ok(result);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<IssueAuditDto>>> GetHistory(Guid id)
    {
        var result = await _mediator.Send(new GetIssueHistoryQuery(id));
        return Ok(result);
    }

    [HttpGet("workflow")]
    public async Task<ActionResult<WorkflowConfigDto>> GetWorkflow()
    {
        var result = await _mediator.Send(new GetIssueWorkflowConfigQuery());
        return Ok(result);
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<IssueDto>> Assign(
        Guid id,
        [FromBody] AssignIssueCommand command,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromHeader(Name = "X-Expected-Version")] string? expectedVersionHeader)
    {
        var expectedVersion = ResolveExpectedVersion(command.ExpectedVersion, ifMatch, expectedVersionHeader);
        var updated = command with { IssueId = id, ExpectedVersion = expectedVersion };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<IssueCommentDto>> AddComment(Guid id, [FromBody] AddCommentCommand command)
    {
        var updated = command with { IssueId = id };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<ActionResult<IssueAttachmentDto>> AttachFile(Guid id, [FromBody] AttachFileRequest request)
    {
        var uploadedByUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(uploadedByUserIdStr, out var uploadedByUserId))
            return Unauthorized();

        var bearerToken = Request.Cookies["accessToken"] ?? string.Empty;

        var command = new AttachFileCommand(id, request.FileId, uploadedByUserId, bearerToken, null);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("{id:guid}/attachments")]
    public async Task<ActionResult<IReadOnlyList<IssueAttachmentDto>>> GetAttachments(Guid id)
    {
        var result = await _mediator.Send(new GetIssueAttachmentsQuery(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<IssueDto>> ChangeStatus(
        Guid id,
        [FromBody] ChangeIssueStatusCommand command,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromHeader(Name = "X-Expected-Version")] string? expectedVersionHeader)
    {
        var expectedVersion = ResolveExpectedVersion(command.ExpectedVersion, ifMatch, expectedVersionHeader);
        var updated = command with { IssueId = id, ExpectedVersion = expectedVersion };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    private static int ResolveExpectedVersion(int expectedVersion, string? ifMatch, string? expectedVersionHeader)
    {
        if (expectedVersion > 0)
            return expectedVersion;

        if (TryParseExpectedVersion(expectedVersionHeader, out var headerValue))
            return headerValue;

        if (TryParseExpectedVersion(ifMatch, out var ifMatchValue))
            return ifMatchValue;

        throw new ValidationException(nameof(expectedVersion), "ExpectedVersion is required. Use body, If-Match, or X-Expected-Version header.");
    }

    private static bool TryParseExpectedVersion(string? value, out int version)
    {
        version = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim().Trim('"');
        return int.TryParse(trimmed, out version) && version > 0;
    }
}

