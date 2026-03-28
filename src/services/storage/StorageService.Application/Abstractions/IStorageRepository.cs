using BitirmeProject.StorageService.Domain.Entities;

namespace BitirmeProject.StorageService.Application.Abstractions;

public interface IStorageRepository
{
    Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoredFile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoredFile>> GetExpiredTemporaryFilesAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task AddAsync(StoredFile file, CancellationToken cancellationToken = default);
    Task UpdateAsync(StoredFile file, CancellationToken cancellationToken = default);
    Task RemoveAsync(StoredFile file, CancellationToken cancellationToken = default);
}
