using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BitirmeProject.Bff.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BitirmeProject.Bff.Api.Controllers;

[ApiController]
[Route("api/v1/bff")]
[Authorize]
public sealed class BffController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ServiceEndpoints _endpoints;

    public BffController(IHttpClientFactory httpClientFactory, IOptions<ServiceEndpoints> endpoints)
    {
        _httpClientFactory = httpClientFactory;
        _endpoints = endpoints.Value;
    }

    [HttpGet("board/{projectId:guid}")]
    public async Task<ActionResult<BoardResponse>> GetBoard(Guid projectId, CancellationToken cancellationToken)
    {
        var projectClient = _httpClientFactory.CreateClient("ProjectService");
        var issueClient = _httpClientFactory.CreateClient("IssueService");

        var (project, projectError) = await GetOrError<ProjectDto>(
            projectClient,
            $"/api/v1/projects/{projectId}",
            cancellationToken);
        if (projectError is not null) return projectError;

        var (items, itemsError) = await GetOrError<List<IssueBoardItemDto>>(
            issueClient,
            $"/api/v1/issues/project/{projectId}",
            cancellationToken);
        if (itemsError is not null) return itemsError;

        var (workflow, workflowError) = await GetOrError<WorkflowConfigDto>(
            issueClient,
            "/api/v1/issues/workflow",
            cancellationToken);
        if (workflowError is not null) return workflowError;

        var response = new BoardResponse
        {
            Project = project,
            Config = BuildBoardConfig(workflow),
            Items = items ?? new List<IssueBoardItemDto>()
        };

        return Ok(response);
    }

    [HttpGet("flags")]
    public ActionResult<UiFlags> GetFlags()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();
        var isAdmin = roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase));
        var hasSystemManager = roles.Any(r => r.Equals("Manager", StringComparison.OrdinalIgnoreCase));
        var hasSystemMember = roles.Any(r => r.Equals("Member", StringComparison.OrdinalIgnoreCase));
        var orgRole = User.FindFirst("org_role")?.Value;
        var isOrgManager = orgRole is "Owner" or "Manager";
        var isOrgMember = !string.IsNullOrWhiteSpace(orgRole);

        var flags = new UiFlags
        {
            CanManageProjects = isAdmin || hasSystemManager || isOrgManager,
            CanEditIssues = isAdmin || hasSystemManager || hasSystemMember || isOrgMember,
            CanAssignIssues = isAdmin || hasSystemManager || isOrgManager,
            CanChangeStatus = isAdmin || hasSystemManager || hasSystemMember || isOrgMember,
            CanViewAdmin = isAdmin
        };

        return Ok(flags);
    }

    [HttpGet("sprint/active/{projectId:guid}")]
    public async Task<ActionResult<SprintDto?>> GetActiveSprint(Guid projectId, CancellationToken cancellationToken)
    {
        var sprintClient = _httpClientFactory.CreateClient("SprintService");

        var (sprint, sprintError) = await GetOrError<SprintDto>(
            sprintClient,
            $"/api/v1/sprints/project/{projectId}/active",
            cancellationToken);
        if (sprintError is not null) return sprintError;

        return Ok(sprint);
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetNotifications(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var notificationClient = _httpClientFactory.CreateClient("NotificationService");

        var (items, error) = await GetOrError<List<NotificationDto>>(
            notificationClient,
            $"/api/v1/notifications/user/{userId}",
            cancellationToken);
        if (error is not null) return error;

        return Ok(items ?? new List<NotificationDto>());
    }

    private static BoardConfig BuildBoardConfig(WorkflowConfigDto? workflow)
    {
        if (workflow is null)
            return new BoardConfig();

        return new BoardConfig
        {
            Columns = workflow.Statuses
                .Select(status => new BoardColumn
                {
                    Key = status,
                    Title = ToDisplayTitle(status),
                    WipLimit = null
                })
                .ToArray(),
            AllowedTransitions = workflow.AllowedTransitions
        };
    }

    private static string ToDisplayTitle(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return status;

        var builder = new StringBuilder(status.Length + 2);
        for (var i = 0; i < status.Length; i++)
        {
            var current = status[i];
            if (i > 0 && char.IsUpper(current) && char.IsLower(status[i - 1]))
                builder.Append(' ');

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    private async Task<(T? Value, ActionResult? Error)> GetOrError<T>(
        HttpClient client,
        string path,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
        }

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return (default, StatusCode((int)response.StatusCode, body));
        }

        var value = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        return (value, null);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                  User.FindFirstValue(ClaimTypes.Name) ??
                  User.FindFirstValue(ClaimTypes.Sid) ??
                  User.FindFirstValue("sub") ??
                  User.FindFirstValue("userId");

        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
