using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Commands.ChangeMemberRole;

public sealed class ChangeMemberRoleCommandHandler : IRequestHandler<ChangeMemberRoleCommand>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeMemberRoleCommandHandler(
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAndUserIdAsync(request.OrganizationId, request.RequestedByUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Organization not found or you are not a member.");

        var requesterRole = organization.Members.FirstOrDefault(m => m.UserId == request.RequestedByUserId)?.Role
            ?? throw new UnauthorizedAccessException("You are not a member of this organization.");

        if (requesterRole != OrganizationRole.Owner)
            throw new UnauthorizedAccessException("Only Owner can change member roles.");

        organization.ChangeMemberRole(request.TargetUserId, request.NewRole);
        await _organizationRepository.UpdateAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
