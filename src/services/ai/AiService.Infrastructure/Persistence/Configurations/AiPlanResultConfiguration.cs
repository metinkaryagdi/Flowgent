using BitirmeProject.AiService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitirmeProject.AiService.Infrastructure.Persistence.Configurations;

public sealed class AiPlanResultConfiguration : IEntityTypeConfiguration<AiPlanResult>
{
    public void Configure(EntityTypeBuilder<AiPlanResult> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.SessionId).IsRequired();
        builder.Property(r => r.Prompt).IsRequired();
        builder.Property(r => r.RawResponse).IsRequired();
        builder.Property(r => r.ParsedJson);
        builder.Property(r => r.WasApplied).IsRequired();

        builder.ToTable("AiPlanResults");
    }
}
