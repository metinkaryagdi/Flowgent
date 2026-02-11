using System.Security.Cryptography;
using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Options;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly JwtOptions _jwtOptions;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IOptions<JwtOptions> options)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _jwtOptions = options.Value;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUserNameAsync(request.UserNameOrEmail, cancellationToken)
                   ?? await _userRepository.GetByEmailAsync(request.UserNameOrEmail, cancellationToken);

        if (user is null)
            throw new InvalidOperationException("Invalid credentials.");

        if (!user.IsActive)
            throw new InvalidOperationException("User is inactive.");

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
            throw new InvalidOperationException("Invalid credentials.");

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
