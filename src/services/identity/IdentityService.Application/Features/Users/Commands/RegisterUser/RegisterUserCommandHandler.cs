using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Entities;
using MediatR;
using AutoMapper;

namespace BitirmeProject.IdentityService.Application.Features.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        // Kullanıcı adı zaten var mı?
        if (await _userRepository.ExistsByUserNameAsync(
                request.UserName,
                null,
                cancellationToken))
        {
            throw new InvalidOperationException("Username already exists.");
        }

        // Email zaten var mı?
        if (await _userRepository.ExistsByEmailAsync(
                request.Email,
                null,
                cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        // Şifreyi hashle
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Yeni kullanıcı oluştur
        var user = new User(request.UserName, request.Email, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // DTO'ya map et
        return _mapper.Map<UserDto>(user);
    }
}
