using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.ReadModels;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Messaging;

namespace BitirmeProject.IssueService.Infrastructure.Persistence;

public sealed class IssueDbContext : DbContext, IUnitOfWork
{
    public IssueDbContext(DbContextOptions<IssueDbContext> options) : base(options) { }

    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueComment> IssueComments => Set<IssueComment>();
    public DbSet<IssueAttachment> IssueAttachments => Set<IssueAttachment>();
    public DbSet<IssueAudit> IssueAudits => Set<IssueAudit>();
    public DbSet<IssueBoardItem> IssueBoardItems => Set<IssueBoardItem>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Priority).HasConversion<int>();
            entity.Property(x => x.Version).IsConcurrencyToken().HasDefaultValue(1);
            entity.HasIndex(x => x.ProjectId);
        });

        modelBuilder.Entity<IssueComment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).IsRequired().HasMaxLength(2000);
            entity.HasIndex(x => x.IssueId);
        });

        modelBuilder.Entity<IssueAttachment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            entity.Property(x => x.ContentType).IsRequired().HasMaxLength(200);
            entity.Property(x => x.SizeBytes).IsRequired();
            entity.HasIndex(x => x.IssueId);
            entity.HasIndex(x => x.FileId);
            // Prevent the same file from being attached to the same issue twice
            entity.HasIndex(x => new { x.IssueId, x.FileId }).IsUnique();
        });

        modelBuilder.Entity<IssueAudit>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FromStatus).HasConversion<int>();
            entity.Property(x => x.ToStatus).HasConversion<int>();
            entity.HasIndex(x => x.IssueId);
        });

        modelBuilder.Entity<IssueBoardItem>(entity =>
        {
            entity.HasKey(x => x.IssueId);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Priority).HasConversion<int>();
            entity.HasIndex(x => x.ProjectId);
            entity.HasIndex(x => x.SprintId);
        });

        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(200);
            entity.HasIndex(x => x.EventId).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
        });
    }

    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
