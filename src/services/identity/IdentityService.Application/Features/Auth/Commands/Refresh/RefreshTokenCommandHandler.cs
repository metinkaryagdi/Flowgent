using System.Security.Cryptography;
using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Options;
using BitirmeProject.IdentityService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Options;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Refresh;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IOptions<JwtOptions> options)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _jwtOptions = options.Value;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _refreshTokenRepository.GetByTokenAsync(TokenHasher.Hash(request.RefreshToken), cancellationToken);
        if (existing is null || !existing.IsActive)
            throw new InvalidOperationException("Invalid refresh token.");

        var user = await _userRepository.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new InvalidOperationException("Invalid refresh token.");

        existing.Revoke();
        await _refreshTokenRepository.UpdateAsync(existing, cancellationToken);

        var roles = user.UserRoles
            .Select(ur => ur.Role?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .Cast<string>()
            .ToList();

        Guid? organizationId = null;
        string? organizationRole = null;
        string? organizationName = null;

        // 1. Explicit org from request (e.g. caller passing current org_id claim) — DB-level member check
        if (request.OrganizationId.HasValue)
        {
            var requestedOrganization = await _organizationRepository.GetByIdAndUserIdAsync(request.OrganizationId.Value, user.Id, cancellationToken);
            if (requestedOrganization is not null)
            {
                organizationId = requestedOrganization.Id;
                organizationRole = requestedOrganization.Members.FirstOrDefault(m => m.UserId == user.Id)?.Role.ToString();
                organizationName = requestedOrganization.Name;
            }
        }

        // 2. User's last-active org — DB-level member check
        if (!organizationId.HasValue && user.LastActiveOrganizationId.HasValue)
        {
            var lastActive = await _organizationRepository.GetByIdAndUserIdAsync(user.LastActiveOrganizationId.Value, user.Id, cancellationToken);
            if (lastActive is not null)
            {
                organizationId = lastActive.Id;
                organizationRole = lastActive.Members.FirstOrDefault(m => m.UserId == user.Id)?.Role.ToString();
                organizationName = lastActive.Name;
            }
        }

        // 3. Any org as fallback
        if (!organizationId.HasValue)
        {
            var defaultOrganization = await _organizationRepository.GetByUserIdAsync(user.Id, cancellationToken);
            organizationId = defaultOrganization?.Id;
            organizationRole = defaultOrganization?.Members.FirstOrDefault(m => m.UserId == user.Id)?.Role.ToString();
            organizationName = defaultOrganization?.Name;
        }

        var token = _jwtTokenGenerator.Generate(user, roles, organizationId, organizationRole);

        var rawNewToken = GenerateToken();
        var newRefreshToken = new RefreshToken(
            user.Id,
            TokenHasher.Hash(rawNewToken),
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays));

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            RefreshToken = rawNewToken,
            User = _mapper.Map<UserDto>(user),
            Roles = roles,
            ActiveOrgId = organizationId,
            ActiveOrgName = organizationName,
            ActiveOrgRole = organizationRole,
        };
    }

    private static string GenerateToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
