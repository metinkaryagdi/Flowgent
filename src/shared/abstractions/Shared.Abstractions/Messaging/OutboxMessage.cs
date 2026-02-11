namespace Shared.Abstractions.Messaging;

/// <summary>
/// Represents an outbox message for reliable event publishing
/// Implements the Transactional Outbox Pattern
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string EventType { get; set; } = string.Empty;
    
    public string Payload { get; set; } = string.Empty;
    
    public DateTime OccurredOn { get; set; }
    
    public DateTime? ProcessedOn { get; set; }
    
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    
    public int RetryCount { get; set; }
    
    public string? Error { get; set; }
}

public enum OutboxStatus
{
    Pending = 0,
    Published = 1,
    Failed = 2
}
