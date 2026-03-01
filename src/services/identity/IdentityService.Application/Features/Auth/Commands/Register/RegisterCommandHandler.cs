using System.Security.Cryptography;
using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Options;
using BitirmeProject.IdentityService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Options;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IMapper _mapper;
    private readonly JwtOptions _jwtOptions;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IMapper mapper,
        IOptions<JwtOptions> options)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _mapper = mapper;
        _jwtOptions = options.Value;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByUserNameAsync(request.UserName, null, cancellationToken))
            throw new InvalidOperationException("Username already exists.");

        if (await _userRepository.ExistsByEmailAsync(request.Email, null, cancellationToken))
            throw new InvalidOperationException("Email already exists.");

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(request.UserName, request.Email, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);

        var defaultRole = await _roleRepository.GetByNameAsync("Viewer", cancellationToken);
        if (defaultRole is not null)
        {
            user.AddRole(defaultRole);
        }

        var roles = user.UserRoles
            .Select(ur => ur.Role?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .Cast<string>()
            .ToList();

        var token = _jwtTokenGenerator.Generate(user, roles);

        var refreshToken = new RefreshToken(
            user.Id,
            GenerateToken(),
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays));

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            RefreshToken = refreshToken.Token,
            User = _mapper.Map<UserDto>(user),
            Roles = roles
        };
    }

    private static string GenerateToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
