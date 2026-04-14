using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.DTOs;
using BitirmeProject.AiService.Infrastructure.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BitirmeProject.AiService.Infrastructure.Clients;

public sealed class SprintServiceClient : ISprintServiceClient
{
    private readonly HttpClient _http;
    private readonly InternalServiceOptions _options;

    public SprintServiceClient(HttpClient http, IOptions<InternalServiceOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<CreatedSprintDto> CreateSprintAsync(
        Guid projectId, Guid userId, Guid organizationId, string name, string goal, CancellationToken ct = default)
    {
        var payload = new { ProjectId = projectId, Name = name, Goal = goal };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sprints");
        AddInternalHeaders(request, userId, organizationId);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<SprintApiResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Sprint service returned null.");

        return new CreatedSprintDto { Id = dto.Id, Name = dto.Name, Goal = dto.Goal ?? string.Empty };
    }

    public async Task AddIssueToSprintAsync(Guid sprintId, Guid issueId, Guid userId, CancellationToken ct = default)
    {
        var payload = new { IssueId = issueId };
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/sprints/{sprintId}/issues");
        AddInternalHeaders(request, userId);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ActiveSprintDto?> GetActiveSprintAsync(Guid projectId, Guid organizationId, CancellationToken ct = default)
    {
        var sprintRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/sprints/project/{projectId}/active");
        AddInternalHeaders(sprintRequest, Guid.Empty, organizationId);
        var sprintResponse = await _http.SendAsync(sprintRequest, ct);
        if (!sprintResponse.IsSuccessStatusCode)
            return null;

        var sprint = await sprintResponse.Content.ReadFromJsonAsync<SprintApiResponse>(cancellationToken: ct);
        if (sprint is null)
            return null;

        var issuesRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/sprints/{sprint.Id}/issues");
        AddInternalHeaders(issuesRequest, Guid.Empty, organizationId);
        var issuesResponse = await _http.SendAsync(issuesRequest, ct);
        issuesResponse.EnsureSuccessStatusCode();

        var issues = await issuesResponse.Content.ReadFromJsonAsync<List<SprintIssueApiResponse>>(cancellationToken: ct);

        return new ActiveSprintDto
        {
            Id = sprint.Id,
            Name = sprint.Name,
            Goal = sprint.Goal,
            Issues = issues?.Select(i => new ActiveSprintIssueDto
            {
                Id = i.IssueId,
                Title = i.Title,
                Status = i.Status,
                Priority = i.Priority
            }).ToList() ?? new()
        };
    }

    public async Task<SprintDetailDto?> GetSprintByIdAsync(Guid sprintId, Guid organizationId, CancellationToken ct = default)
    {
        var sprintRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/sprints/{sprintId}");
        AddInternalHeaders(sprintRequest, Guid.Empty, organizationId);
        var sprintResponse = await _http.SendAsync(sprintRequest, ct);
        if (!sprintResponse.IsSuccessStatusCode) return null;

        var sprint = await sprintResponse.Content.ReadFromJsonAsync<SprintApiResponse>(cancellationToken: ct);
        if (sprint is null)
            return null;

        var issuesRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/sprints/{sprintId}/issues");
        AddInternalHeaders(issuesRequest, Guid.Empty, organizationId);
        var issuesResponse = await _http.SendAsync(issuesRequest, ct);
        issuesResponse.EnsureSuccessStatusCode();

        var issues = await issuesResponse.Content.ReadFromJsonAsync<List<SprintIssueApiResponse>>(cancellationToken: ct);

        return new SprintDetailDto
        {
            Id = sprintId,
            Name = sprint.Name,
            Goal = sprint.Goal,
            Status = sprint.Status,
            Issues = issues?.Select(i => new SprintDetailIssueDto
            {
                Id = i.IssueId,
                Title = i.Title,
                Status = i.Status,
                Priority = i.Priority
            }).ToList() ?? new()
        };
    }

    private void AddInternalHeaders(HttpRequestMessage request, Guid userId, Guid? organizationId = null, string? organizationRole = null)
    {
        request.Headers.TryAddWithoutValidation("X-Internal-Service", _options.CallerName);
        request.Headers.TryAddWithoutValidation("X-Internal-Service-Key", _options.ApiKey);
        request.Headers.TryAddWithoutValidation("X-User-Id", userId.ToString());

        if (organizationId.HasValue)
            request.Headers.TryAddWithoutValidation("X-Organization-Id", organizationId.Value.ToString());

        if (!string.IsNullOrWhiteSpace(organizationRole))
            request.Headers.TryAddWithoutValidation("X-Organization-Role", organizationRole);
    }

    private sealed class SprintApiResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Goal { get; set; }
        public string Status { get; set; } = null!;
    }

    private sealed class SprintIssueApiResponse
    {
        public Guid IssueId { get; set; }
        public string Title { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Priority { get; set; } = null!;
    }
}
