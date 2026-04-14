using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using BitirmeProject.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.IdentityService.Infrastructure.Persistence;

/// <summary>
/// Seeds a default admin user on first startup.
/// Credentials: admin / Admin@123
/// </summary>
public static class AdminUserSeeder
{
    private const string AdminUserName = "admin";
    private const string AdminEmail    = "admin@bitirme.local";
    private const string AdminPassword = "Admin@123";

    public static async Task SeedAsync(
        IdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        // Idempotent: skip if any admin user already exists
        var adminRole = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == DefaultIdentityRoles.Admin, cancellationToken);

        if (adminRole is null)
            return; // roles not seeded yet — should not happen since RoleSeeder runs first

        var hasAdmin = await dbContext.UserRoles
            .AnyAsync(ur => ur.RoleId == adminRole.Id, cancellationToken);

        if (hasAdmin)
            return;

        var passwordHash = passwordHasher.HashPassword(AdminPassword);
        var admin = new User(AdminUserName, AdminEmail, passwordHash);
        admin.AddRole(adminRole);

        await dbContext.Users.AddAsync(admin, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
