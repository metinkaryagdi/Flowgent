namespace BitirmeProject.StorageService.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveTemporaryAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
    Task<string> PromoteAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ListPathsAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
