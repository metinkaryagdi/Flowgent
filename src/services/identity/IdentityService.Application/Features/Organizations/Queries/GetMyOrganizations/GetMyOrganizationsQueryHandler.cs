using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetMyOrganizations;

public sealed class GetMyOrganizationsQueryHandler
    : IRequestHandler<GetMyOrganizationsQuery, List<OrganizationDto>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMapper _mapper;

    public GetMyOrganizationsQueryHandler(IOrganizationRepository organizationRepository, IMapper mapper)
    {
        _organizationRepository = organizationRepository;
        _mapper = mapper;
    }

    public async Task<List<OrganizationDto>> Handle(
        GetMyOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        var organizations = await _organizationRepository.GetAllByUserIdAsync(request.UserId, cancellationToken);

        return _mapper.Map<List<OrganizationDto>>(organizations);
    }
}
