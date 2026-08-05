using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using BitirmeProject.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BitirmeProject.IdentityService.Infrastructure.Persistence;

public static class AdminUserSeeder
{
    public static async Task SeedAsync(
        IdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var seedAdmin = configuration["SEED_ADMIN"];
        if (string.Equals(seedAdmin, "false", StringComparison.OrdinalIgnoreCase))
            return;

        var adminPassword = configuration["ADMIN_PASSWORD"]
            ?? throw new InvalidOperationException(
                "ADMIN_PASSWORD environment variable is required. Set SEED_ADMIN=false to disable seeding.");

        var adminEmail = configuration["ADMIN_EMAIL"] ?? "admin@bitirme.local";
        var adminUserName = configuration["ADMIN_USERNAME"] ?? "admin";

        var adminRole = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == DefaultIdentityRoles.Admin, cancellationToken);

        if (adminRole is null)
            return;

        var hasAdmin = await dbContext.UserRoles
            .AnyAsync(ur => ur.RoleId == adminRole.Id, cancellationToken);

        if (hasAdmin)
            return;

        var passwordHash = passwordHasher.HashPassword(adminPassword);
        var admin = new User(adminUserName, adminEmail, passwordHash);
        // The address comes from ADMIN_EMAIL in the operator's own environment, so there is
        // nothing to prove and no mailbox to wait on -- a seeded admin that had to click a
        // link would deadlock a fresh deployment if SMTP were not configured yet.
        admin.MarkEmailVerifiedOnCreation();
        admin.AddRole(adminRole);

        await dbContext.Users.AddAsync(admin, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
