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
        // DB-level member check: returns org only if requesting user is a member
        var organization = await _organizationRepository.GetByIdAndUserIdAsync(request.OrganizationId, request.RequestedByUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Organization not found or you are not a member.");

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
