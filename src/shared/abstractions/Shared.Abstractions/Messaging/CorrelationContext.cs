namespace Shared.Abstractions.Messaging;

/// <summary>
/// Carries correlation and actor information across the request pipeline.
/// Populated by middleware from incoming HTTP headers or message headers.
/// </summary>
public class CorrelationContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The authenticated user's Id extracted from JWT Claims — NOT from request body.
    /// </summary>
    public string? ActorId { get; set; }
}
