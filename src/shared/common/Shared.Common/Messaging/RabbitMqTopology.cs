namespace Shared.Common.Messaging;

/// <summary>
/// Canonical RabbitMQ topology names used by service-specific consumers.
/// </summary>
public static class RabbitMqTopology
{
    public const string EventsExchangeName = "bitirme_events";
    public const string DeadLetterExchangeName = "bitirme_events.dlx";

    public static string GetQueueName(string serviceName, string eventType) =>
        $"{serviceName}.{eventType}.queue";

    public static string GetDeadLetterQueueName(string serviceName, string eventType) =>
        $"{serviceName}.{eventType}.dlq";

    public static string GetDeadLetterRoutingKey(string serviceName, string eventType) =>
        $"{serviceName}.{eventType}";
}
