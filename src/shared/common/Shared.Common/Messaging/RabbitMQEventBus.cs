using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
    private readonly string _exchangeName = "bitirme_events";

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

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = eventType;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published event {EventType} to exchange {Exchange}",
            eventType, _exchangeName);

        return Task.CompletedTask;
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IIntegrationEvent
        where THandler : IEventHandler<TEvent>
    {
        var eventType = typeof(TEvent).Name;
        var queueName = $"{eventType}_queue";

        // Declare queue
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        // Bind queue to exchange
        _channel.QueueBind(
            queue: queueName,
            exchange: _exchangeName,
            routingKey: eventType);

        _logger.LogInformation(
            "Subscribed to event {EventType} with queue {QueueName}",
            eventType, queueName);

        // Note: Actual message consumption should be done through a BackgroundService
        // This method just sets up the queue binding
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
