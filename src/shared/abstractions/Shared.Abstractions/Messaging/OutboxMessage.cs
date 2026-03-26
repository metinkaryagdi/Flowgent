namespace Shared.Abstractions.Messaging;

/// <summary>
/// Represents an outbox message for reliable event publishing.
/// Implements the Transactional Outbox Pattern with optimistic claim/lock support.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    /// <summary>CorrelationId propagated from the originating request.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>ActorId (UserId) that triggered the event — from trusted Claims context.</summary>
    public string? ActorId { get; set; }

    public DateTime OccurredOn { get; set; }

    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;

    public int RetryCount { get; set; }

    /// <summary>Last error message when Status == Failed.</summary>
    public string? LastError { get; set; }

    /// <summary>UTC timestamp when the message was successfully published.</summary>
    public DateTime? PublishedOn { get; set; }

    /// <summary>UTC timestamp when the message was last attempted to be published.</summary>
    public DateTime? LastAttemptedAt { get; set; }

    /// <summary>
    /// When not null, this worker instance has claimed this message.
    /// Other instances must not process claimed messages whose ClaimedUntil is in the future.
    /// </summary>
    public Guid? LockId { get; set; }

    /// <summary>Claim expires at this UTC time. After expiry, the message is available again.</summary>
    public DateTime? ClaimedUntil { get; set; }

    /// <summary>Earliest UTC time this message should be retried after a failure.</summary>
    public DateTime? NextRetryAt { get; set; }
}

public enum OutboxStatus
{
    Pending = 0,
    Processing = 1,
    Published = 2,
    Failed = 3
}
