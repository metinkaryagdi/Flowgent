using System.Net.Http.Json;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.IssueService.Infrastructure.Clients;

public sealed class StorageServiceClient : IStorageServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StorageServiceClient> _logger;

    public StorageServiceClient(IHttpClientFactory httpClientFactory, ILogger<StorageServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<StorageFileMetadataDto?> GetFileMetadataAsync(
        Guid fileId,
        string? bearerToken,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("StorageService");

        if (!string.IsNullOrWhiteSpace(bearerToken))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

        try
        {
            var response = await client.GetAsync($"api/v1/storage/files/{fileId}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<StorageFileMetadataDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "StorageService call failed for fileId {FileId}", fileId);
            throw;
        }
    }
}
