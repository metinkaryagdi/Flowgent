using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Revoke;

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeTokenCommandHandler(IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(
            TokenHasher.Hash(request.RefreshToken), cancellationToken);

        if (token is null || !token.IsActive)
            return; // Already revoked or invalid — nothing to do

        token.Revoke();
        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
