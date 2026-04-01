using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Entities;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByUserNameAsync(request.UserName, null, cancellationToken))
            throw new InvalidOperationException("Username already exists.");

        if (await _userRepository.ExistsByEmailAsync(request.Email, null, cancellationToken))
            throw new InvalidOperationException("Email already exists.");

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(request.UserName, request.Email, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);

        var defaultRole = await _roleRepository.GetByNameAsync(DefaultIdentityRoles.Default, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Default role '{DefaultIdentityRoles.Default}' is not configured.");

        user.AddRole(defaultRole);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }
}
