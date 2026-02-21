using BitirmeProject.StorageService.Application.Abstractions;

namespace BitirmeProject.StorageService.Infrastructure.FileSystem;

public sealed class FileSystemStorage : IFileStorage
{
    private readonly string _rootPath;

    public FileSystemStorage(string rootPath)
    {
        _rootPath = rootPath;
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var safeName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var path = Path.Combine(_rootPath, safeName);

        await using var fileStream = File.Create(path);
        await content.CopyToAsync(fileStream, cancellationToken);

        return safeName;
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_rootPath, storagePath);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(path);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_rootPath, storagePath);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}
