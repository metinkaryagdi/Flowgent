using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Domain.Common;
using BitirmeProject.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Abstractions.Messaging;
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
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<InviteToken> InviteTokens => Set<InviteToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
        });

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

    async Task<IUnitOfWorkTransaction> IUnitOfWork.BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    private sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfUnitOfWorkTransaction(IDbContextTransaction transaction) => _transaction = transaction;

        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            _transaction.CommitAsync(cancellationToken);

        // EF rolls an uncommitted transaction back on dispose, so a handler that throws
        // partway through needs no explicit rollback call.
        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
