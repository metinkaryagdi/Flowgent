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
            organization = await _organizationRepository.GetByIdAsync(request.OrganizationId.Value, cancellationToken);
            // Verify user is actually a member of this org
            if (organization is not null && !organization.Members.Any(m => m.UserId == request.UserId))
                organization = null;
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
