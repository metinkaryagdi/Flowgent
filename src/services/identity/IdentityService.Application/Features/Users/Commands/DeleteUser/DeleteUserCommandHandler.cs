using BitirmeProject.IdentityService.Application.Abstractions;
using MediatR;

namespace IdentityService.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Admin-side deactivation. This is a soft delete: the row keeps its email and username,
/// so it is not erasure and cannot answer a deletion request. That is DeleteMyAccount.
/// </summary>
public sealed class DeleteUserCommandHandler
    : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            throw new KeyNotFoundException($"User not found. UserId={request.UserId}");

        // idempotent
        if (user.IsDeleted)
            return Unit.Value;

        await _userRepository.DeleteAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

}


