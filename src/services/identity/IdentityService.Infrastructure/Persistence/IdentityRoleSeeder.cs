using BitirmeProject.IdentityService.Application.Common;
using BitirmeProject.IdentityService.Domain.Common;
using BitirmeProject.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.IdentityService.Infrastructure.Persistence;

public static class IdentityRoleSeeder
{
    public static async Task SeedAsync(
        IdentityDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var requiredRoleNames = DefaultIdentityRoles.All
            .Select(role => role.Name)
            .ToArray();

        var existingRoles = await dbContext.Roles
            .IgnoreQueryFilters()
            .Where(role => requiredRoleNames.Contains(role.Name))
            .ToListAsync(cancellationToken);

        var restoredAnyRole = existingRoles.Any(role => role.IsDeleted);

        foreach (var softDeletedRole in existingRoles.Where(role => role.IsDeleted))
        {
            dbContext.Entry(softDeletedRole).Property(nameof(BaseEntity.IsDeleted)).CurrentValue = false;
            dbContext.Entry(softDeletedRole).Property(nameof(BaseEntity.DeletedAt)).CurrentValue = null;
            dbContext.Entry(softDeletedRole).Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
        }

        var existingNames = existingRoles
            .Select(role => role.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingRoles = DefaultIdentityRoles.All
            .Where(role => !existingNames.Contains(role.Name))
            .Select(role => new Role(role.Name, role.Description))
            .ToList();

        if (missingRoles.Count > 0)
            await dbContext.Roles.AddRangeAsync(missingRoles, cancellationToken);

        var defaultRole = existingRoles
            .Concat(missingRoles)
            .First(role => role.Name.Equals(DefaultIdentityRoles.Default, StringComparison.OrdinalIgnoreCase));

        var rolelessUsers = await dbContext.Users
            .Include(user => user.UserRoles)
            .Where(user => !user.UserRoles.Any())
            .ToListAsync(cancellationToken);

        foreach (var user in rolelessUsers)
            user.AddRole(defaultRole);

        if (missingRoles.Count == 0 && !restoredAnyRole && rolelessUsers.Count == 0)
            return;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
