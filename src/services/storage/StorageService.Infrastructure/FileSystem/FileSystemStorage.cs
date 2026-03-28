using BitirmeProject.StorageService.Application.Abstractions;

namespace BitirmeProject.StorageService.Infrastructure.FileSystem;

public sealed class FileSystemStorage : IFileStorage
{
    private readonly string _rootPath;
    private readonly string _temporaryRootPath;
    private readonly string _permanentRootPath;

    public FileSystemStorage(string rootPath)
    {
        _rootPath = rootPath;
        _temporaryRootPath = Path.Combine(_rootPath, "temp");
        _permanentRootPath = Path.Combine(_rootPath, "files");

        Directory.CreateDirectory(_temporaryRootPath);
        Directory.CreateDirectory(_permanentRootPath);
    }

    public async Task<string> SaveTemporaryAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var safeName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var relativePath = NormalizeRelativePath(Path.Combine("temp", safeName));
        var path = ResolvePath(relativePath);

        await using var fileStream = File.Create(path);
        await content.CopyToAsync(fileStream, cancellationToken);

        return relativePath;
    }

    public Task<string> PromoteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var relativePath = NormalizeRelativePath(storagePath);
        if (relativePath.StartsWith("files/", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(relativePath);

        var sourcePath = ResolvePath(relativePath);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Temporary file was not found.", sourcePath);

        var targetRelativePath = NormalizeRelativePath(Path.Combine("files", Path.GetFileName(relativePath)));
        var targetPath = ResolvePath(targetRelativePath);

        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        File.Move(sourcePath, targetPath, overwrite: true);
        return Task.FromResult(targetRelativePath);
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storagePath);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(path);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(ResolvePath(storagePath)));
    }

    public Task<IReadOnlyList<string>> ListPathsAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_rootPath))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var paths = Directory.GetFiles(_rootPath, "*", SearchOption.AllDirectories)
            .Select(path => NormalizeRelativePath(Path.GetRelativePath(_rootPath, path)))
            .ToArray();

        return Task.FromResult<IReadOnlyList<string>>(paths);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storagePath);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    private string ResolvePath(string storagePath)
    {
        var relativePath = NormalizeRelativePath(storagePath);
        var fullRootPath = Path.GetFullPath(_rootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var resolvedPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        if (!resolvedPath.StartsWith(fullRootPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved storage path is outside the configured storage root.");

        return resolvedPath;
    }

    private static string NormalizeRelativePath(string storagePath)
    {
        return storagePath
            .Replace('\\', '/')
            .TrimStart('/');
    }
}
