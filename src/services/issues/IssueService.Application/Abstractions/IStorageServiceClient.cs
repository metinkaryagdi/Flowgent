using BitirmeProject.IssueService.Application.DTOs;

namespace BitirmeProject.IssueService.Application.Abstractions;

public interface IStorageServiceClient
{
    /// <summary>
    /// Returns file metadata from StorageService, or null if the file is not found.
    /// The caller must forward the user's Bearer token so the request is authenticated.
    /// </summary>
    Task<StorageFileMetadataDto?> GetFileMetadataAsync(
        Guid fileId,
        string? bearerToken,
        CancellationToken cancellationToken = default);
}
