namespace Shared.Abstractions.Messaging;

/// <summary>
/// Repository interface for managing outbox messages with optimistic claim/lock support.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>Adds a new outbox message atomically within the current transaction.</summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims a batch of pending messages for this worker instance (lockId).
    /// Sets Status = Processing and ClaimedUntil = now + lockDuration.
    /// Returns only the claimed messages.
    /// </summary>
    Task<List<OutboxMessage>> ClaimBatchAsync(
        Guid lockId,
        int batchSize,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending messages to be published (legacy — prefer ClaimBatchAsync).
    /// </summary>
    Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Marks a message as successfully published.</summary>
    Task MarkAsPublishedAsync(Guid messageId, DateTime publishedOn, CancellationToken cancellationToken = default);

    /// <summary>Marks a message as failed and schedules a retry.</summary>
    Task MarkAsFailedAsync(Guid messageId, string error, DateTime? nextRetryAt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases orphaned claims whose ClaimedUntil has expired back to Pending status.
    /// Called at startup or periodically to recover from crashed workers.
    /// </summary>
    Task ReleaseOrphanClaimsAsync(CancellationToken cancellationToken = default);
}
