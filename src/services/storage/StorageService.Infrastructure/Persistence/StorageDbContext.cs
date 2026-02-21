using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.StorageService.Infrastructure.Persistence;

public sealed class StorageDbContext : DbContext, IUnitOfWork
{
    public StorageDbContext(DbContextOptions<StorageDbContext> options) : base(options) { }

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            entity.Property(x => x.ContentType).IsRequired().HasMaxLength(200);
            entity.Property(x => x.StoragePath).IsRequired().HasMaxLength(500);
            entity.Property(x => x.SizeBytes).IsRequired();
            entity.HasIndex(x => x.UploadedByUserId);
        });
    }

    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
