namespace BitirmeProject.IdentityService.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an explicit transaction. Needed when a handler mixes tracked changes with
    /// set-based deletes, which execute immediately instead of waiting for SaveChanges --
    /// account erasure does exactly that, and half-erasing an account is not an acceptable
    /// failure mode.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>Commits or rolls back on dispose. Disposing without committing rolls back.</summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
