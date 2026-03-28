using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Domain.Entities;
using BitirmeProject.StorageService.Domain.Enums;
using BitirmeProject.StorageService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.StorageService.Infrastructure.Repositories;

public sealed class StorageRepository : IStorageRepository
{
    private readonly StorageDbContext _dbContext;

    public StorageRepository(StorageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StoredFiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StoredFile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.StoredFiles
            .OrderBy(x => x.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredFile>> GetExpiredTemporaryFilesAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StoredFiles
            .Where(x => x.Status == StoredFileStatus.Temporary
                && x.ExpiresAt.HasValue
                && x.ExpiresAt <= utcNow)
            .OrderBy(x => x.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        await _dbContext.StoredFiles.AddAsync(file, cancellationToken);
    }

    public Task UpdateAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        _dbContext.StoredFiles.Update(file);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        _dbContext.StoredFiles.Remove(file);
        return Task.CompletedTask;
    }
}
