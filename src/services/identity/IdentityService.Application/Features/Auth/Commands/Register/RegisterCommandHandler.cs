using System.Security.Cryptography;
using System.Text.Json;
using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Options;
using BitirmeProject.IdentityService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;
    private readonly JwtOptions _jwtOptions;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOutboxRepository outboxRepository,
        IMapper mapper,
        IOptions<JwtOptions> options)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
        _jwtOptions = options.Value;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedUserName = request.UserName.ToLowerInvariant();
        var normalizedEmail    = request.Email.ToLowerInvariant();

        if (await _userRepository.ExistsByUserNameAsync(normalizedUserName, null, cancellationToken))
            throw new InvalidOperationException("Username already exists.");

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, null, cancellationToken))
            throw new InvalidOperationException("Email already exists.");

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(normalizedUserName, normalizedEmail, passwordHash);

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

        var rawToken = GenerateToken();
        var refreshToken = new RefreshToken(
            user.Id,
            TokenHasher.Hash(rawToken),
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays));

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        var evt = new UserCreatedEvent(user.Id, user.UserName, user.Email, Guid.Empty);
        await _outboxRepository.AddAsync(new OutboxMessage
        {
            EventType = evt.GetType().Name,
            Payload = JsonSerializer.Serialize(evt),
            OccurredOn = evt.OccurredOn
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            RefreshToken = rawToken,
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
