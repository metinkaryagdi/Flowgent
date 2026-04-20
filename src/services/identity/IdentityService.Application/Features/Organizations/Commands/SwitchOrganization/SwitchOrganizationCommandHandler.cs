using BitirmeProject.IdentityService.Application.Abstractions;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Commands.SwitchOrganization;

public sealed class SwitchOrganizationCommandHandler
    : IRequestHandler<SwitchOrganizationCommand, SwitchOrganizationResult>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public SwitchOrganizationCommandHandler(
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<SwitchOrganizationResult> Handle(
        SwitchOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        // DB-level member check: returns org only if user is a member
        var organization = await _organizationRepository.GetByIdAndUserIdAsync(request.OrganizationId, request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Organization not found or user is not a member.");

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var roles = user.UserRoles
            .Select(ur => ur.Role?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .Cast<string>()
            .ToList();

        var orgRole = organization.Members.FirstOrDefault(m => m.UserId == request.UserId)?.Role.ToString()
            ?? throw new InvalidOperationException("Could not determine member role.");

        // Persist so subsequent logins/refreshes restore the same org context
        user.SetActiveOrganization(organization.Id);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenGenerator.Generate(user, roles, organization.Id, orgRole);

        return new SwitchOrganizationResult(
            token.AccessToken,
            token.ExpiresAt,
            organization.Name,
            orgRole);
    }
}
