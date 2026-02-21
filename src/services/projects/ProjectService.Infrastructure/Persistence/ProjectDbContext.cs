using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Messaging;

namespace BitirmeProject.ProjectService.Infrastructure.Persistence;

public sealed class ProjectDbContext : DbContext, IUnitOfWork
{
    public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Key).IsRequired().HasMaxLength(10);
            entity.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(x => new { x.ProjectId, x.UserId });
            entity.HasIndex(x => x.UserId);
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
