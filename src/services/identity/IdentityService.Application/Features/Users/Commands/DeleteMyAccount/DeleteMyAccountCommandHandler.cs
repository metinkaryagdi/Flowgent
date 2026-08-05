using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.IdentityService.Application.Features.Users.Commands.DeleteMyAccount;

public sealed class DeleteMyAccountCommandHandler : IRequestHandler<DeleteMyAccountCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccountErasureStore _erasureStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteMyAccountCommandHandler> _logger;

    public DeleteMyAccountCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAccountErasureStore erasureStore,
        IUnitOfWork unitOfWork,
        ILogger<DeleteMyAccountCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _erasureStore = erasureStore;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteMyAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            throw new NotFoundException("User", request.UserId);

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            _logger.LogWarning(
                "Account deletion refused: password confirmation failed. UserId={UserId}",
                request.UserId);
            throw new UnauthorizedAccessException("Password is incorrect.");
        }

        // Losing the last admin would leave the deployment with nobody able to administer
        // it, and this is irreversible -- there is no undo once the address is gone.
        if (IsAdmin(user) && !await HasAnotherActiveAdminAsync(user.Id, cancellationToken))
        {
            throw new BusinessRuleException(
                "You are the only active administrator. Promote another administrator before deleting your account.");
        }

        var email = user.Email;

        // The purge runs set-based deletes, which hit the database immediately rather than
        // waiting for SaveChanges. Without one transaction around both halves, a failure in
        // between would leave an account stripped of its memberships but still holding the
        // address it asked to have erased.
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var removedRows = await _erasureStore.PurgeAccountArtifactsAsync(user.Id, email, cancellationToken);

        user.Anonymize();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        // Deliberately logs the id and not the address: writing the address here would
        // just move the personal data from the database into the log store.
        _logger.LogInformation(
            "Account erased on the owner's request. UserId={UserId} RemovedRows={RemovedRows}",
            user.Id,
            removedRows);

        return Unit.Value;
    }

    private async Task<bool> HasAnotherActiveAdminAsync(Guid excludedUserId, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Any(other => other.Id != excludedUserId && other.IsActive && IsAdmin(other));
    }

    private static bool IsAdmin(global::User user) =>
        user.UserRoles.Any(userRole =>
            userRole.Role?.Name.Equals(DefaultIdentityRoles.Admin, StringComparison.OrdinalIgnoreCase) == true);
}
