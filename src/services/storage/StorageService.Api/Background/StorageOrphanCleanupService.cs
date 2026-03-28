using BitirmeProject.StorageService.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.StorageService.Api.Background;

public sealed class StorageOrphanCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly StorageCleanupMonitor _monitor;
    private readonly ILogger<StorageOrphanCleanupService> _logger;
    private readonly TimeSpan _interval;

    public StorageOrphanCleanupService(
        IServiceScopeFactory scopeFactory,
        StorageCleanupMonitor monitor,
        ILogger<StorageOrphanCleanupService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _monitor = monitor;
        _logger = logger;

        var intervalMinutes = configuration.GetValue<int?>("Storage:CleanupIntervalMinutes") ?? 15;
        _interval = TimeSpan.FromMinutes(Math.Max(5, intervalMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _monitor.RecordFailure(DateTime.UtcNow, ex);
                _logger.LogError(ex, "Storage orphan cleanup failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;
        _monitor.RecordStarted(startedAt);

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IStorageRepository>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var expiredFiles = await repository.GetExpiredTemporaryFilesAsync(DateTime.UtcNow, cancellationToken);
        var expiredDeleteCount = 0;

        foreach (var file in expiredFiles)
        {
            await fileStorage.DeleteAsync(file.StoragePath, cancellationToken);
            await repository.RemoveAsync(file, cancellationToken);
            expiredDeleteCount++;
        }

        if (expiredDeleteCount > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        var allFiles = await repository.GetAllAsync(cancellationToken);
        var metadataPaths = allFiles
            .Select(file => file.StoragePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allBinaryPaths = await fileStorage.ListPathsAsync(cancellationToken);
        var orphanTempPaths = allBinaryPaths
            .Where(path => path.StartsWith("temp/", StringComparison.OrdinalIgnoreCase))
            .Where(path => !metadataPaths.Contains(path))
            .ToArray();

        foreach (var orphanPath in orphanTempPaths)
            await fileStorage.DeleteAsync(orphanPath, cancellationToken);

        var missingBinaryCount = 0;
        foreach (var file in allFiles)
        {
            if (!await fileStorage.ExistsAsync(file.StoragePath, cancellationToken))
                missingBinaryCount++;
        }

        var completedAt = DateTime.UtcNow;
        _monitor.RecordCompleted(
            completedAt,
            expiredDeleteCount,
            orphanTempPaths.Length,
            missingBinaryCount);

        _logger.LogInformation(
            "Storage cleanup cycle completed. ExpiredTempDeletes={ExpiredTempDeletes}, OrphanTempDeletes={OrphanTempDeletes}, MissingBinaryCount={MissingBinaryCount}",
            expiredDeleteCount,
            orphanTempPaths.Length,
            missingBinaryCount);
    }
}
