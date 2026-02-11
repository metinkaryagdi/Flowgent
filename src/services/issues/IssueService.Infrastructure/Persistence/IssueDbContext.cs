using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Messaging;

namespace BitirmeProject.IssueService.Infrastructure.Persistence;

public sealed class IssueDbContext : DbContext, IUnitOfWork
{
    public IssueDbContext(DbContextOptions<IssueDbContext> options) : base(options) { }

    public DbSet<Issue> Issues => Set<Issue>();
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
            entity.HasIndex(x => x.ProjectId);
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