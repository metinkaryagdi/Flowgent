using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Domain.Entities;
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

    public async Task AddAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        await _dbContext.StoredFiles.AddAsync(file, cancellationToken);
    }

    public Task RemoveAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        _dbContext.StoredFiles.Remove(file);
        return Task.CompletedTask;
    }
}
