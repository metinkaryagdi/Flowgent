using BitirmeProject.AiService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitirmeProject.AiService.Infrastructure.Persistence.Configurations;

public sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(128);
        builder.Property(e => e.ProcessedOn).IsRequired();
        builder.HasIndex(e => e.EventId).IsUnique();
        builder.ToTable("ProcessedEvents");
    }
}
