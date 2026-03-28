using System.Text.Json;

namespace Shared.Common.Messaging;

public readonly record struct IntegrationEventMetadata(
    Guid EventId,
    Guid CorrelationId,
    int EventVersion);

public static class IntegrationEventMetadataExtractor
{
    public static bool TryExtract(string payload, out IntegrationEventMetadata metadata)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            metadata = new IntegrationEventMetadata(
                TryReadGuid(root, "EventId"),
                TryReadGuid(root, "CorrelationId"),
                TryReadInt32(root, "EventVersion", 1));

            return true;
        }
        catch
        {
            metadata = default;
            return false;
        }
    }

    private static Guid TryReadGuid(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return Guid.Empty;

        if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var parsed))
            return parsed;

        return Guid.Empty;
    }

    private static int TryReadInt32(JsonElement root, string propertyName, int defaultValue)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return defaultValue;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsed))
            return parsed;

        return defaultValue;
    }
}
