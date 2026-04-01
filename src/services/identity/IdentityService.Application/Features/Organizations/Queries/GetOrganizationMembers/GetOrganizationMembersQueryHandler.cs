using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetOrganizationMembers;

public sealed class GetOrganizationMembersQueryHandler
    : IRequestHandler<GetOrganizationMembersQuery, IReadOnlyList<OrganizationMemberDto>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;

    public GetOrganizationMembersQueryHandler(
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository)
    {
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<OrganizationMemberDto>> Handle(
        GetOrganizationMembersQuery request,
        CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken)
            ?? throw new InvalidOperationException("Organization not found.");

        if (!organization.HasMember(request.RequestedByUserId))
            throw new UnauthorizedAccessException("You are not a member of this organization.");

        var result = new List<OrganizationMemberDto>();
        foreach (var member in organization.Members)
        {
            var user = await _userRepository.GetByIdAsync(member.UserId, cancellationToken);
            if (user is null) continue;

            result.Add(new OrganizationMemberDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = member.Role.ToString(),
                JoinedAt = member.JoinedAt
            });
        }

        return result;
    }
}
