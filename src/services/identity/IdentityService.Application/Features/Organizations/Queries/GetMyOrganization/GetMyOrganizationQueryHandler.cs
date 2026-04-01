using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetMyOrganization;

public sealed class GetMyOrganizationQueryHandler
    : IRequestHandler<GetMyOrganizationQuery, OrganizationDto?>
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetMyOrganizationQueryHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task<OrganizationDto?> Handle(
        GetMyOrganizationQuery request,
        CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (organization is null)
            return null;

        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            CreatedByUserId = organization.CreatedByUserId,
            CreatedAt = organization.CreatedAt,
            MemberCount = organization.Members.Count
        };
    }
}
