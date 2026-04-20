using System.Security.Cryptography;
using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Options;
using BitirmeProject.IdentityService.Domain.Entities;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Options;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly JwtOptions _jwtOptions;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IOptions<JwtOptions> options)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _jwtOptions = options.Value;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedInput = request.UserNameOrEmail.ToLowerInvariant();
        var user = await _userRepository.GetByUserNameAsync(normalizedInput, cancellationToken)
                   ?? await _userRepository.GetByEmailAsync(normalizedInput, cancellationToken);

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

        // Prefer the last-active org; fall back to any org the user belongs to.
        // Uses DB-level member check to avoid relying on in-memory _members collection.
        Organization? organization = null;
        if (user.LastActiveOrganizationId.HasValue)
            organization = await _organizationRepository.GetByIdAndUserIdAsync(user.LastActiveOrganizationId.Value, user.Id, cancellationToken);

        organization ??= await _organizationRepository.GetByUserIdAsync(user.Id, cancellationToken);

        Guid? orgId = organization?.Id;
        // Determine role directly from the loaded Members collection (populated by Include).
        string? orgRole = organization?.Members.FirstOrDefault(m => m.UserId == user.Id)?.Role.ToString();
        string? orgName = organization?.Name;

        var token = _jwtTokenGenerator.Generate(user, roles, orgId, orgRole);

        var rawToken = GenerateToken();
        var refreshToken = new RefreshToken(
            user.Id,
            HashToken(rawToken),
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays));

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            RefreshToken = rawToken,
            User = _mapper.Map<UserDto>(user),
            Roles = roles,
            ActiveOrgId = orgId,
            ActiveOrgName = orgName,
            ActiveOrgRole = orgRole,
        };
    }

    private static string GenerateToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    internal static string HashToken(string rawToken) => TokenHasher.Hash(rawToken);
}
