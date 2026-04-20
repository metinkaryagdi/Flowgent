using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Commands.AcceptInviteExisting;

public sealed class AcceptInviteExistingCommandHandler
    : IRequestHandler<AcceptInviteExistingCommand, UserDto>
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AcceptInviteExistingCommandHandler(
        IInviteRepository inviteRepository,
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _inviteRepository = inviteRepository;
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(AcceptInviteExistingCommand request, CancellationToken cancellationToken)
    {
        var invite = await _inviteRepository.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new InvalidOperationException("Invite token not found.");

        if (!invite.IsValid)
            throw new InvalidOperationException("Invite token is expired or already used.");

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (!string.Equals(user.Email, invite.Email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This invite was sent to a different email address.");

        var organization = await _organizationRepository.GetByIdAsync(invite.OrganizationId, cancellationToken)
            ?? throw new InvalidOperationException("Organization not found.");

        if (organization.Members.Any(m => m.UserId == user.Id))
            throw new InvalidOperationException("You are already a member of this organization.");

        organization.AddMember(user.Id, invite.Role);
        await _organizationRepository.UpdateAsync(organization, cancellationToken);

        invite.MarkAsUsed();
        await _inviteRepository.UpdateAsync(invite, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }
}
