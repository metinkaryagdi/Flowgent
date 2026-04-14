using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Entities;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Commands.AcceptInvite;

public sealed class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, UserDto>
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AcceptInviteCommandHandler(
        IInviteRepository inviteRepository,
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _inviteRepository = inviteRepository;
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        var invite = await _inviteRepository.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new InvalidOperationException("Invite token not found.");

        if (!invite.IsValid)
            throw new InvalidOperationException("Invite token is expired or already used.");

        if (await _userRepository.ExistsByUserNameAsync(request.UserName, null, cancellationToken))
            throw new InvalidOperationException("Username already exists.");

        if (await _userRepository.ExistsByEmailAsync(invite.Email, null, cancellationToken))
            throw new InvalidOperationException("A user with this email already exists.");

        var organization = await _organizationRepository.GetByIdAsync(invite.OrganizationId, cancellationToken)
            ?? throw new InvalidOperationException("Organization not found.");

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(request.UserName, invite.Email, passwordHash);

        var defaultRole = await _roleRepository.GetByNameAsync(DefaultIdentityRoles.Default, cancellationToken)
            ?? throw new InvalidOperationException($"Default role '{DefaultIdentityRoles.Default}' is not configured.");

        user.AddRole(defaultRole);
        await _userRepository.AddAsync(user, cancellationToken);

        organization.AddMember(user.Id, invite.Role);
        await _organizationRepository.UpdateAsync(organization, cancellationToken);

        invite.MarkAsUsed();
        await _inviteRepository.UpdateAsync(invite, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }
}
