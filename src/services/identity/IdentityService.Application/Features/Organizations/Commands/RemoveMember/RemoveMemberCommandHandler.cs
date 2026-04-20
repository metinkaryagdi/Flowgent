using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Commands.RemoveMember;

public sealed class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveMemberCommandHandler(
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAndUserIdAsync(request.OrganizationId, request.RequestedByUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Organization not found or you are not a member.");

        var requesterRole = organization.Members.FirstOrDefault(m => m.UserId == request.RequestedByUserId)?.Role
            ?? throw new UnauthorizedAccessException("You are not a member of this organization.");

        if (requesterRole == OrganizationRole.Member)
            throw new UnauthorizedAccessException("Only Owner or Manager can remove members.");

        organization.RemoveMember(request.TargetUserId);
        await _organizationRepository.UpdateAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
