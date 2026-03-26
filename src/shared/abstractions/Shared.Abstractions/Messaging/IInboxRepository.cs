namespace Shared.Abstractions.Messaging;

/// <summary>
/// Repository interface for inbox-based idempotency tracking.
/// Each consumer checks this before processing to prevent duplicate handling.
/// </summary>
public interface IInboxRepository
{
    /// <summary>
    /// Returns true if this event has already been processed by the given consumer.
    /// </summary>
    Task<bool> ExistsAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that this event has been processed by the given consumer.
    /// Must be called within the same transaction as the business handler.
    /// </summary>
    Task AddAsync(InboxEntry entry, CancellationToken cancellationToken = default);
}
