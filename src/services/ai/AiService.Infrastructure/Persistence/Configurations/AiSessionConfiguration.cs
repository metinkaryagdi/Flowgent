using BitirmeProject.AiService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitirmeProject.AiService.Infrastructure.Persistence.Configurations;

public sealed class AiSessionConfiguration : IEntityTypeConfiguration<AiSession>
{
    public void Configure(EntityTypeBuilder<AiSession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProjectId).IsRequired();
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.OrganizationId).IsRequired();
        builder.Property(s => s.Type).HasConversion<string>().IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().IsRequired();
        builder.Property(s => s.ErrorMessage).HasMaxLength(1000);

        builder.HasMany(s => s.Results)
            .WithOne(r => r.Session)
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("AiSessions");
    }
}
