using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Commands.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name,
    Guid CreatedByUserId) : IRequest<OrganizationDto>;
