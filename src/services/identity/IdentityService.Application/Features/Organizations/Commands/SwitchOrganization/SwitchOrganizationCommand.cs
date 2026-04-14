using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Commands.SwitchOrganization;

public sealed record SwitchOrganizationCommand(Guid UserId, Guid OrganizationId) : IRequest<SwitchOrganizationResult>;

public sealed record SwitchOrganizationResult(
    string AccessToken,
    DateTime ExpiresAt,
    string OrgName,
    string OrgRole);
