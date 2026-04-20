using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.ReadModels;
using BitirmeProject.SprintService.Domain.Entities;
using BitirmeProject.SprintService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Messaging;

namespace BitirmeProject.SprintService.Infrastructure.Persistence;

public sealed class SprintDbContext : DbContext, IUnitOfWork
{
    public SprintDbContext(DbContextOptions<SprintDbContext> options) : base(options) { }

    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<SprintIssue> SprintIssues => Set<SprintIssue>();
    public DbSet<SprintSummary> SprintSummaries => Set<SprintSummary>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Sprint>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Goal).HasMaxLength(2000);
            entity.Property(x => x.StartDate).IsRequired();
            entity.Property(x => x.EndDate).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => x.ProjectId)
                .HasFilter($"\"Status\" = {(int)SprintStatus.Active}")
                .IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
        });

        modelBuilder.Entity<SprintIssue>(entity =>
        {
            entity.HasKey(x => x.IssueId);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.IssueType).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Priority).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(x => x.ProjectId);
            entity.HasIndex(x => x.SprintId);
            entity.HasIndex(x => x.OrganizationId);
        });

        modelBuilder.Entity<SprintSummary>(entity =>
        {
            entity.HasKey(x => x.SprintId);
            entity.HasIndex(x => x.SprintId).IsUnique();
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
