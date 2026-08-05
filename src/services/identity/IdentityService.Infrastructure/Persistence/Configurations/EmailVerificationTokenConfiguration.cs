using BitirmeProject.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitirmeProject.IdentityService.Infrastructure.Persistence.Configurations;

public sealed class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("email_verification_tokens");

        builder.HasKey(x => x.Id);

        // SHA-256 as lowercase hex is always 64 chars.
        builder.Property(x => x.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        // Unique: the lookup on the verification endpoint goes through this index, and it
        // also makes an accidental duplicate insert a hard failure rather than an
        // ambiguous multi-row match.
        builder.HasIndex(x => x.TokenHash).IsUnique();

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.IsUsed).IsRequired();
        builder.Property(x => x.UsedAt).IsRequired(false);

        // Supports "retire this user's outstanding tokens" on resend.
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
