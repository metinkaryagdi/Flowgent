using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Entities;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetMyOrganization;

public sealed class GetMyOrganizationQueryHandler
    : IRequestHandler<GetMyOrganizationQuery, OrganizationDto?>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMapper _mapper;

    public GetMyOrganizationQueryHandler(IOrganizationRepository organizationRepository, IMapper mapper)
    {
        _organizationRepository = organizationRepository;
        _mapper = mapper;
    }

    public async Task<OrganizationDto?> Handle(
        GetMyOrganizationQuery request,
        CancellationToken cancellationToken)
    {
        Organization? organization;
        if (request.OrganizationId.HasValue)
        {
            // DB-level member check: returns null if user is not a member
            organization = await _organizationRepository.GetByIdAndUserIdAsync(request.OrganizationId.Value, request.UserId, cancellationToken);
        }
        else
        {
            organization = await _organizationRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        }

        if (organization is null)
            return null;

        return _mapper.Map<OrganizationDto>(organization);
    }
}
