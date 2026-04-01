using BitirmeProject.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitirmeProject.IdentityService.Infrastructure.Persistence.Configurations;

public class InviteTokenConfiguration : IEntityTypeConfiguration<InviteToken>
{
    public void Configure(EntityTypeBuilder<InviteToken> builder)
    {
        builder.ToTable("InviteTokens");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Token)
            .IsRequired();

        builder.HasIndex(i => i.Token)
            .IsUnique();

        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.OrganizationId)
            .IsRequired();

        builder.Property(i => i.InvitedByUserId)
            .IsRequired();

        builder.Property(i => i.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.ExpiresAt)
            .IsRequired();

        builder.Property(i => i.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.UsedAt)
            .IsRequired(false);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .IsRequired(false);

        builder.Property(i => i.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.DeletedAt)
            .IsRequired(false);

        builder.HasOne(i => i.Organization)
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
