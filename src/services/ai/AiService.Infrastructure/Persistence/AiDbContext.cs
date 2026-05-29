using BitirmeProject.AiService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.AiService.Infrastructure.Persistence;

public sealed class AiDbContext : DbContext
{
    public AiDbContext(DbContextOptions<AiDbContext> options) : base(options) { }

    public DbSet<AiSession> AiSessions => Set<AiSession>();
    public DbSet<AiPlanResult> AiPlanResults => Set<AiPlanResult>();
    public DbSet<AiToolExecution> AiToolExecutions => Set<AiToolExecution>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AiDbContext).Assembly);
    }
}
