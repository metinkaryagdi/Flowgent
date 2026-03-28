using BitirmeProject.SprintService.Application.DTOs;

namespace BitirmeProject.SprintService.Application.Abstractions;

public interface IIssueServiceClient
{
    /// <summary>
    /// Returns minimal issue metadata from IssueService, or null if the issue is not found.
    /// The caller must forward the user's Bearer token so the request is authenticated.
    /// </summary>
    Task<IssueMetadataDto?> GetIssueAsync(
        Guid issueId,
        string? bearerToken,
        CancellationToken cancellationToken = default);
}
