using BitirmeProject.AiService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitirmeProject.AiService.Infrastructure.Persistence.Configurations;

public sealed class AiToolExecutionConfiguration : IEntityTypeConfiguration<AiToolExecution>
{
    public void Configure(EntityTypeBuilder<AiToolExecution> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SessionId);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.OrganizationId).IsRequired();
        builder.Property(e => e.ProjectId).IsRequired();
        builder.Property(e => e.ToolName).IsRequired().HasMaxLength(64);
        builder.Property(e => e.InputJson).IsRequired();
        builder.Property(e => e.OutputJson);
        builder.Property(e => e.Success).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.DurationMs).IsRequired();

        builder.HasIndex(e => new { e.OrganizationId, e.ProjectId, e.CreatedAt })
            .HasDatabaseName("IX_AiToolExecutions_Org_Project_CreatedAt");
        builder.HasIndex(e => e.SessionId)
            .HasDatabaseName("IX_AiToolExecutions_SessionId");

        builder.ToTable("AiToolExecutions");
    }
}
