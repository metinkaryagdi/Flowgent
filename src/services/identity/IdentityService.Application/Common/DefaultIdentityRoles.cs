namespace BitirmeProject.IdentityService.Application.Common;

public static class DefaultIdentityRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Member = "Member";
    public const string Default = Member;

    public static IReadOnlyList<RoleDefinition> All { get; } =
    [
        new RoleDefinition(Admin, "System administrator with full access."),
        new RoleDefinition(Manager, "Project manager with elevated project permissions."),
        new RoleDefinition(Member, "Standard member role assigned to newly registered users.")
    ];

    public sealed record RoleDefinition(string Name, string? Description);
}
