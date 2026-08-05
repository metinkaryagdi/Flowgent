using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Unit>
{
    private readonly IEmailVerificationTokenRepository _tokens;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IEmailVerificationTokenRepository tokens,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _tokens = tokens;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        // Look up by hash: only the hash is stored, so a stolen database cannot be replayed.
        var tokenHash = TokenHasher.Hash(request.Token);
        var token = await _tokens.GetByTokenHashAsync(tokenHash, cancellationToken);

        // One message for "no such token", "already used" and "expired". Distinguishing them
        // would let someone probe which tokens ever existed.
        if (token is null || !token.IsValid)
        {
            _logger.LogWarning("Email verification failed: token missing, used or expired.");
            throw new InvalidOperationException("Verification link is invalid or has expired.");
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Email verification token references a missing user. UserId={UserId}", token.UserId);
            throw new InvalidOperationException("Verification link is invalid or has expired.");
        }

        // The address is pinned at issue time, so a token minted for the old address cannot
        // confirm a new one if the user changed it in between.
        if (!string.Equals(user.Email, token.Email, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Email verification token address no longer matches the account. UserId={UserId}",
                user.Id);
            throw new InvalidOperationException("Verification link is invalid or has expired.");
        }

        token.MarkAsUsed();
        await _tokens.UpdateAsync(token, cancellationToken);

        user.ConfirmEmail();
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email verified. UserId={UserId} Status={Status}", user.Id, user.Status);

        return Unit.Value;
    }
}
