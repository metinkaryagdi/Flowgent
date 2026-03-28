using System.Net.Http.Json;
using System.Text.Json;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.SprintService.Infrastructure.Clients;

public sealed class IssueServiceClient : IIssueServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IssueServiceClient> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public IssueServiceClient(IHttpClientFactory httpClientFactory, ILogger<IssueServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IssueMetadataDto?> GetIssueAsync(
        Guid issueId,
        string? bearerToken,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("IssueService");

        if (!string.IsNullOrWhiteSpace(bearerToken))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

        try
        {
            var response = await client.GetAsync($"api/v1/issues/{issueId}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            // IssueService returns IssueDto; we only map the fields we need.
            var raw = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

            return new IssueMetadataDto(
                Id: raw.GetProperty("id").GetGuid(),
                ProjectId: raw.GetProperty("projectId").GetGuid(),
                Title: raw.GetProperty("title").GetString() ?? string.Empty,
                Status: raw.GetProperty("status").ToString(),
                Priority: raw.GetProperty("priority").ToString(),
                CreatedByUserId: raw.GetProperty("createdByUserId").GetGuid());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "IssueService call failed for IssueId {IssueId}", issueId);
            throw;
        }
    }
}
