using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Commands.RemoveMember;

public sealed record RemoveMemberCommand(
    Guid OrganizationId,
    Guid RequestedByUserId,
    Guid TargetUserId) : IRequest;
