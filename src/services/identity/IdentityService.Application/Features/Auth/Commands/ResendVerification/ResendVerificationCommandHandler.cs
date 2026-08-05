using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.ResendVerification;

public sealed class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationIssuer _issuer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResendVerificationCommandHandler> _logger;

    public ResendVerificationCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationIssuer issuer,
        IUnitOfWork unitOfWork,
        ILogger<ResendVerificationCommandHandler> logger)
    {
        _userRepository = userRepository;
        _issuer = issuer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        // Returns success regardless of what was found. Reporting "no such account" or
        // "already verified" would turn this endpoint into an account-enumeration oracle:
        // anyone could test an address list and learn who is registered.
        if (user is null)
        {
            _logger.LogInformation("Verification resend requested for an address with no account.");
            return Unit.Value;
        }

        if (user.IsEmailVerified)
        {
            _logger.LogInformation("Verification resend requested for an already-verified account. UserId={UserId}", user.Id);
            return Unit.Value;
        }

        await _issuer.IssueAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Verification email re-sent. UserId={UserId}", user.Id);

        return Unit.Value;
    }
}
