using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Domain.Entities;
using BitirmeProject.NotificationService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Messaging;

namespace BitirmeProject.NotificationService.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext, IUnitOfWork
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.Channel).HasConversion<int>();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.IsRead).IsRequired();
            entity.Property(x => x.DeliveryAttemptCount).IsRequired();
            entity.Property(x => x.LastDeliveryAttemptAt);
            entity.Property(x => x.NextDeliveryAttemptAt);
            entity.Property(x => x.DeliveredAt);
            entity.Property(x => x.LastFailureReason).HasMaxLength(2000);
            entity.Property(x => x.EntityType).HasMaxLength(100);
            entity.Property(x => x.ExternalEventId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.ExternalEventId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.NextDeliveryAttemptAt);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
        });

        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(200);
            entity.HasIndex(x => x.EventId).IsUnique();
        });
    }

    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
