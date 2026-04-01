using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Abstractions.Messaging;
using Shared.Common.Options;

namespace Shared.Common.Messaging;

/// <summary>
/// RabbitMQ implementation of IEventBus
/// </summary>
public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly RabbitMQOptions _options;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName = RabbitMqTopology.EventsExchangeName;

    public RabbitMQEventBus(
        ILogger<RabbitMQEventBus> logger,
        IOptions<RabbitMQOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange
        _channel.ExchangeDeclare(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Enable publisher confirms so we know when the broker has accepted each message
        _channel.ConfirmSelect();

        _logger.LogInformation("RabbitMQ EventBus initialized. Exchange: {Exchange}", _exchangeName);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var eventType = @event.GetType().Name;
        var message = JsonSerializer.Serialize(@event);

        await PublishRawAsync(eventType, message, cancellationToken);
    }

    public Task PublishRawAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        var routingKey = eventType;
        var body = Encoding.UTF8.GetBytes(payload);
        IntegrationEventMetadataExtractor.TryExtract(payload, out var metadata);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = eventType;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.MessageId = metadata.EventId == Guid.Empty ? null : metadata.EventId.ToString();
        properties.CorrelationId = metadata.CorrelationId == Guid.Empty ? null : metadata.CorrelationId.ToString();
        properties.Headers = new Dictionary<string, object>
        {
            ["x-event-version"] = metadata.EventVersion,
            ["x-event-id"] = metadata.EventId == Guid.Empty ? string.Empty : metadata.EventId.ToString(),
            ["x-correlation-id"] = metadata.CorrelationId == Guid.Empty ? string.Empty : metadata.CorrelationId.ToString()
        };

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        // Wait for broker acknowledgment; throws if nacked or timeout exceeded
        _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

        _logger.LogInformation(
            "Published event {EventType} to exchange {Exchange}. EventId={EventId}, EventVersion={EventVersion}, CorrelationId={CorrelationId}",
            eventType,
            _exchangeName,
            metadata.EventId == Guid.Empty ? null : metadata.EventId,
            metadata.EventVersion,
            metadata.CorrelationId == Guid.Empty ? null : metadata.CorrelationId);

        return Task.CompletedTask;
    }

    [Obsolete("Legacy generic queue topology is disabled. Declare service-specific queues inside a BackgroundService consumer.")]
    public void Subscribe<TEvent, THandler>()
        where TEvent : IIntegrationEvent
        where THandler : IEventHandler<TEvent>
    {
        throw new NotSupportedException(
            "Legacy generic queue topology is disabled. " +
            "Declare service-specific queues inside a BackgroundService consumer.");
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        
        _logger.LogInformation("RabbitMQ EventBus disposed");
    }
}
