using System.Security.Claims;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.AddComment;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.AssignIssue;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.AttachFile;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.ChangeIssueStatus;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.CreateIssue;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.DeleteIssue;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.UpdateIssue;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueAttachments;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueById;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueComments;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueHistory;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByAssignee;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProject;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProjectPaged;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesBySprint;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueWorkflowConfig;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Abstractions.Exceptions;
using Shared.Common.Extensions;

namespace BitirmeProject.IssueService.Api.Controllers;

[ApiController]
[Route("api/v1/issues")]
[Authorize]
public sealed class IssuesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IIssueRepository _issueRepository;

    public IssuesController(IMediator mediator, IIssueRepository issueRepository)
    {
        _mediator = mediator;
        _issueRepository = issueRepository;
    }

    [HttpPost]
    public async Task<ActionResult<IssueDto>> Create([FromBody] CreateIssueCommand command)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
            return Unauthorized();

        var updated = command with { CreatedByUserId = userId.Value, OrganizationId = ResolveCallerOrgId() };
        var result = await _mediator.Send(updated);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IssueDto>> GetById(Guid id)
    {
        var (_, error) = await AuthorizeIssueScopeAsync(id);
        if (error is not null)
            return error;

        var result = await _mediator.Send(new GetIssueByIdQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("project/{projectId:guid}")]
    public async Task<ActionResult<IReadOnlyList<IssueBoardItemDto>>> GetByProject(Guid projectId)
    {
        var callerOrgId = ResolveCallerOrgId();
        if (!User.HasRole("Admin") && !User.IsInternalCall() && !callerOrgId.HasValue)
            return Forbid();

        var result = await _mediator.Send(new GetIssuesByProjectQuery(projectId, callerOrgId));
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

        var callerOrgId = ResolveCallerOrgId();
        if (!User.HasRole("Admin") && !User.IsInternalCall() && !callerOrgId.HasValue)
            return Forbid();

        var result = await _mediator.Send(new GetIssuesByProjectPagedQuery(projectId, page, pageSize, sprintId, backlogOnly, callerOrgId));
        return Ok(result);
    }

    [HttpGet("assignee/{assigneeUserId:guid}")]
    public async Task<ActionResult<IReadOnlyList<IssueDto>>> GetByAssignee(Guid assigneeUserId)
    {
        var requesterId = User.TryGetUserId();
        if (!User.HasRole("Admin") && !User.IsInternalCall() && requesterId != assigneeUserId)
            return Forbid();

        var result = await _mediator.Send(new GetIssuesByAssigneeQuery(assigneeUserId, ResolveCallerOrgId()));
        return Ok(result);
    }

    [HttpGet("sprint/{sprintId:guid}")]
    public async Task<ActionResult<IReadOnlyList<IssueDto>>> GetBySprint(Guid sprintId)
    {
        var callerOrgId = ResolveCallerOrgId();
        if (!User.HasRole("Admin") && !User.IsInternalCall() && !callerOrgId.HasValue)
            return Forbid();

        var result = await _mediator.Send(new GetIssuesBySprintQuery(sprintId, callerOrgId));
        return Ok(result);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<IssueAuditDto>>> GetHistory(Guid id)
    {
        var (_, error) = await AuthorizeIssueScopeAsync(id);
        if (error is not null)
            return error;

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
        var (_, error) = await AuthorizeIssueMutationAsync(id);
        if (error is not null)
            return error;

        var assignedByUserId = User.TryGetUserId();
        if (assignedByUserId is null)
            return Unauthorized();

        var expectedVersion = ResolveExpectedVersion(command.ExpectedVersion, ifMatch, expectedVersionHeader);
        var updated = command with { IssueId = id, ExpectedVersion = expectedVersion, AssignedByUserId = assignedByUserId.Value };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<IssueCommentDto>> AddComment(Guid id, [FromBody] AddCommentCommand command)
    {
        var (_, error) = await AuthorizeIssueMutationAsync(id);
        if (error is not null)
            return error;

        var authorUserId = User.TryGetUserId();
        if (authorUserId is null)
            return Unauthorized();

        var updated = command with { IssueId = id, AuthorUserId = authorUserId.Value };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<IReadOnlyList<IssueCommentDto>>> GetComments(Guid id)
    {
        var (_, error) = await AuthorizeIssueScopeAsync(id);
        if (error is not null)
            return error;

        var result = await _mediator.Send(new GetIssueCommentsQuery(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<ActionResult<IssueAttachmentDto>> AttachFile(Guid id, [FromBody] AttachFileRequest request)
    {
        var (_, error) = await AuthorizeIssueMutationAsync(id);
        if (error is not null)
            return error;

        var uploadedByUserId = User.TryGetUserId();
        if (uploadedByUserId is null)
            return Unauthorized();

        var bearerToken = Request.Cookies["accessToken"] ?? string.Empty;

        var command = new AttachFileCommand(id, request.FileId, uploadedByUserId.Value, bearerToken, null);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("{id:guid}/attachments")]
    public async Task<ActionResult<IReadOnlyList<IssueAttachmentDto>>> GetAttachments(Guid id)
    {
        var (_, error) = await AuthorizeIssueScopeAsync(id);
        if (error is not null)
            return error;

        var result = await _mediator.Send(new GetIssueAttachmentsQuery(id));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<IssueDto>> Update(
        Guid id,
        [FromBody] UpdateIssueCommand command,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromHeader(Name = "X-Expected-Version")] string? expectedVersionHeader)
    {
        var (_, error) = await AuthorizeIssueMutationAsync(id);
        if (error is not null)
            return error;

        var expectedVersion = ResolveExpectedVersion(command.ExpectedVersion, ifMatch, expectedVersionHeader);
        var updated = command with { IssueId = id, ExpectedVersion = expectedVersion };
        var result = await _mediator.Send(updated);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (_, error) = await AuthorizeIssueMutationAsync(id);
        if (error is not null)
            return error;

        await _mediator.Send(new DeleteIssueCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<IssueDto>> ChangeStatus(
        Guid id,
        [FromBody] ChangeIssueStatusCommand command,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromHeader(Name = "X-Expected-Version")] string? expectedVersionHeader)
    {
        var (_, error) = await AuthorizeIssueMutationAsync(id);
        if (error is not null)
            return error;

        var userId = User.TryGetUserId();
        if (userId is null)
            return Unauthorized();

        var expectedVersion = ResolveExpectedVersion(command.ExpectedVersion, ifMatch, expectedVersionHeader);
        var updated = command with { IssueId = id, ExpectedVersion = expectedVersion, ChangedByUserId = userId.Value };
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

    private Guid? ResolveCallerOrgId()
        => Guid.TryParse(Request.Headers["X-Organization-Id"].FirstOrDefault()
            ?? User.FindFirstValue("org_id"), out var orgId) ? orgId : null;

    private async Task<(BitirmeProject.IssueService.Domain.Entities.Issue? Issue, ActionResult? Error)> AuthorizeIssueScopeAsync(Guid issueId)
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var issue = await _issueRepository.GetByIdAsync(issueId, cancellationToken);
        if (issue is null)
            return (null, NotFound());

        if (User.HasRole("Admin") || User.IsInternalCall())
            return (issue, null);

        var callerOrgId = ResolveCallerOrgId();
        if (issue.OrganizationId.HasValue && issue.OrganizationId != callerOrgId)
            return (null, Forbid());

        return (issue, null);
    }

    private async Task<(BitirmeProject.IssueService.Domain.Entities.Issue? Issue, ActionResult? Error)> AuthorizeIssueMutationAsync(Guid issueId)
    {
        var (issue, error) = await AuthorizeIssueScopeAsync(issueId);
        if (error is not null || issue is null)
            return (issue, error);

        if (User.HasRole("Admin") || User.IsInternalCall())
            return (issue, null);

        var callerId = User.TryGetUserId();
        var isOrgManager = User.GetOrganizationRole() is "Owner" or "Manager";
        var isParticipant = callerId.HasValue && (issue.CreatedByUserId == callerId.Value || issue.AssigneeUserId == callerId.Value);

        return isOrgManager || isParticipant ? (issue, null) : (null, Forbid());
    }
}
