using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.DTOs;
using BitirmeProject.AiService.Infrastructure.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BitirmeProject.AiService.Infrastructure.Clients;

public sealed class IssueServiceClient : IIssueServiceClient
{
    private readonly HttpClient _http;
    private readonly InternalServiceOptions _options;

    public IssueServiceClient(HttpClient http, IOptions<InternalServiceOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<CreatedIssueDto> CreateIssueAsync(
        Guid projectId, Guid userId, Guid organizationId,
        string title, string? description, string priority,
        CancellationToken ct = default)
    {
        var payload = new
        {
            ProjectId = projectId,
            Title = title,
            Description = description,
            Priority = priority,
            OrganizationId = organizationId
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/issues");
        AddInternalHeaders(request, userId, organizationId);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<IssueApiResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Issue service returned null.");

        return new CreatedIssueDto { Id = dto.Id, Title = dto.Title, Priority = dto.Priority };
    }

    public async Task<List<ProjectIssueDto>> GetIssuesByProjectAsync(
        Guid projectId, Guid organizationId, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/issues/project/{projectId}");
        AddInternalHeaders(request, Guid.Empty, organizationId);
        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<IssueListApiResponse>>(cancellationToken: ct);
        return items?.Select(i => new ProjectIssueDto { Id = i.Id, Title = i.Title }).ToList()
               ?? new List<ProjectIssueDto>();
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

    private sealed class IssueApiResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Priority { get; set; } = null!;
    }

    private sealed class IssueListApiResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
    }
}
