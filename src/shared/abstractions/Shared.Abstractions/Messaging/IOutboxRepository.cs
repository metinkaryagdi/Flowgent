namespace Shared.Abstractions.Messaging;

/// <summary>
/// Repository interface for managing outbox messages
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Adds a new outbox message
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets pending messages to be published
    /// </summary>
    Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a message as successfully published
    /// </summary>
    Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a message as failed
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}
