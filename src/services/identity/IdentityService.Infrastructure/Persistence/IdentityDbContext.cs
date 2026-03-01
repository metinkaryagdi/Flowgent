using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Domain.Common;
using BitirmeProject.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BitirmeProject.IdentityService.Infrastructure.Persistence;

public class IdentityDbContext : DbContext, IUnitOfWork
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // Soft Delete (Global Query Filter): BaseEntity türevlerinde IsDeleted == false filtrele
        ApplySoftDeleteQueryFilter(modelBuilder);
    }

    private static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        // BaseEntity’den türeyen tüm entity’leri yakala
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => t.ClrType != null && typeof(BaseEntity).IsAssignableFrom(t.ClrType))
            .Select(t => t.ClrType)
            .Distinct()
            .ToList();

        foreach (var clrType in entityTypes)
        {
            // e =>
            var parameter = Expression.Parameter(clrType, "e");

            // ((BaseEntity)e).IsDeleted
            var isDeletedProperty = Expression.Property(
                Expression.Convert(parameter, typeof(BaseEntity)),
                nameof(BaseEntity.IsDeleted));

            // !((BaseEntity)e).IsDeleted
            var body = Expression.Equal(isDeletedProperty, Expression.Constant(false));

            // e => !e.IsDeleted
            var lambda = Expression.Lambda(body, parameter);

            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }

    // IUnitOfWork implementasyonu
    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
