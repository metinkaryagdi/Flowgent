using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetMyOrganization;

public sealed record GetMyOrganizationQuery(Guid UserId) : IRequest<OrganizationDto?>;
