using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;

namespace Shared.Common.Logging;

public static class IntegrationEventLoggingExtensions
{
    public static IDisposable BeginIntegrationEventScope(
        this ILogger logger,
        IIntegrationEvent @event,
        string consumerName,
        Guid? entityId = null,
        Guid? actorId = null)
    {
        var scope = new Dictionary<string, object?>
        {
            ["CorrelationId"] = @event.CorrelationId == Guid.Empty ? null : @event.CorrelationId,
            ["EventId"] = @event.EventId,
            ["EventVersion"] = @event.EventVersion,
            ["ConsumerName"] = consumerName,
            ["EntityId"] = entityId,
            ["ActorId"] = actorId
        };

        return logger.BeginScope(scope);
    }
}
