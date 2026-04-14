using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetMyOrganizations;

public sealed record GetMyOrganizationsQuery(Guid UserId) : IRequest<List<OrganizationDto>>;
