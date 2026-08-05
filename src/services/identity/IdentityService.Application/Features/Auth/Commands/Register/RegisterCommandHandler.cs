using System.Text.Json;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Entities;
using MediatR;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;

/// <summary>
/// Self-service registration. Creates the account in <c>Pending</c> and emails a
/// verification link; it deliberately issues no access or refresh token, so an
/// unverified address can never reach an authenticated endpoint.
/// </summary>
public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRoleRepository _roleRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEmailVerificationIssuer _verificationIssuer;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IRoleRepository roleRepository,
        IOutboxRepository outboxRepository,
        IEmailVerificationIssuer verificationIssuer)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _roleRepository = roleRepository;
        _outboxRepository = outboxRepository;
        _verificationIssuer = verificationIssuer;
    }

    public async Task<RegisterResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedUserName = request.UserName.ToLowerInvariant();
        var normalizedEmail    = request.Email.ToLowerInvariant();

        if (await _userRepository.ExistsByUserNameAsync(normalizedUserName, null, cancellationToken))
            throw new InvalidOperationException("Username already exists.");

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, null, cancellationToken))
            throw new InvalidOperationException("Email already exists.");

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(normalizedUserName, normalizedEmail, passwordHash);

        // Pending, not Active. LoginCommandHandler and both JwtBearer OnTokenValidated
        // hooks already refuse non-Active users, so this is what actually blocks sign-in.
        user.RequireEmailVerification();

        await _userRepository.AddAsync(user, cancellationToken);

        var defaultRole = await _roleRepository.GetByNameAsync(DefaultIdentityRoles.Default, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Default role '{DefaultIdentityRoles.Default}' is not configured.");

        user.AddRole(defaultRole);

        var evt = new UserCreatedEvent(user.Id, user.UserName, user.Email, Guid.Empty);
        await _outboxRepository.AddAsync(new OutboxMessage
        {
            EventType = evt.GetType().Name,
            Payload = JsonSerializer.Serialize(evt),
            OccurredOn = evt.OccurredOn
        }, cancellationToken);

        await _verificationIssuer.IssueAsync(user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            VerificationRequired = true
        };
    }
}
