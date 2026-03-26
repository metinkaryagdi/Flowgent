using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Domain.Entities;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Api.Events;

public sealed class IssueEventsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IssueEventsConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "bitirme_events";
    private const string ServiceName = "ProjectService";
    private const string DlxName = "bitirme_events.dlx";

    public IssueEventsConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<IssueEventsConsumer> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        InitializeRabbit();
        _channel!.BasicQos(0, 10, false);

        var eventTypes = new[]
        {
            nameof(IssueCreatedEvent),
            nameof(IssueStatusChangedEvent),
            nameof(IssueAssignedEvent)
        };

        // Declare Dead Letter Exchange for failed messages
        _channel.ExchangeDeclare(exchange: DlxName, type: ExchangeType.Topic, durable: true, autoDelete: false);

        foreach (var eventType in eventTypes)
        {
            var dlqName = $"{ServiceName}.{eventType}.dlq";
            _channel.QueueDeclare(queue: dlqName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: dlqName, exchange: DlxName, routingKey: $"{ServiceName}.{eventType}");

            var queueName = $"{ServiceName}.{eventType}.queue";
            var queueArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = DlxName,
                ["x-dead-letter-routing-key"] = $"{ServiceName}.{eventType}"
            };
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
            _channel.QueueBind(queue: queueName, exchange: ExchangeName, routingKey: eventType);
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;

        foreach (var eventType in eventTypes)
        {
            var queueName = $"{ServiceName}.{eventType}.queue";
            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }

        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        return HandleMessageInternalAsync(args);
    }

    private async Task HandleMessageInternalAsync(BasicDeliverEventArgs args)
    {
        var eventType = args.BasicProperties?.Type ?? args.RoutingKey;
        var message = Encoding.UTF8.GetString(args.Body.ToArray());

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processedRepo = scope.ServiceProvider.GetRequiredService<IProcessedEventRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            switch (eventType)
            {
                case nameof(IssueCreatedEvent):
                {
                    var evt = JsonSerializer.Deserialize<IssueCreatedEvent>(message);
                    if (evt is null) throw new InvalidOperationException("Invalid IssueCreatedEvent payload");

                    _logger.LogInformation(
                        "IssueCreatedEvent received. EventId={EventId}, CorrelationId={CorrelationId}",
                        evt.EventId,
                        evt.CorrelationId);

                    if (await processedRepo.ExistsAsync(evt.EventId))
                    {
                        _logger.LogInformation("Duplicate IssueCreatedEvent ignored. EventId={EventId}", evt.EventId);
                        break;
                    }

                    var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<IssueCreatedEvent>>();
                    await handler.HandleAsync(evt);

                    await processedRepo.AddAsync(new ProcessedEvent(evt.EventId, eventType));
                    await unitOfWork.SaveChangesAsync(CancellationToken.None);
                    break;
                }
                case nameof(IssueStatusChangedEvent):
                {
                    var evt = JsonSerializer.Deserialize<IssueStatusChangedEvent>(message);
                    if (evt is null) throw new InvalidOperationException("Invalid IssueStatusChangedEvent payload");

                    _logger.LogInformation(
                        "IssueStatusChangedEvent received. EventId={EventId}, CorrelationId={CorrelationId}",
                        evt.EventId,
                        evt.CorrelationId);

                    if (await processedRepo.ExistsAsync(evt.EventId))
                    {
                        _logger.LogInformation("Duplicate IssueStatusChangedEvent ignored. EventId={EventId}", evt.EventId);
                        break;
                    }

                    var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<IssueStatusChangedEvent>>();
                    await handler.HandleAsync(evt);

                    await processedRepo.AddAsync(new ProcessedEvent(evt.EventId, eventType));
                    await unitOfWork.SaveChangesAsync(CancellationToken.None);
                    break;
                }
                case nameof(IssueAssignedEvent):
                {
                    var evt = JsonSerializer.Deserialize<IssueAssignedEvent>(message);
                    if (evt is null) throw new InvalidOperationException("Invalid IssueAssignedEvent payload");

                    _logger.LogInformation(
                        "IssueAssignedEvent received. EventId={EventId}, CorrelationId={CorrelationId}",
                        evt.EventId,
                        evt.CorrelationId);

                    if (await processedRepo.ExistsAsync(evt.EventId))
                    {
                        _logger.LogInformation("Duplicate IssueAssignedEvent ignored. EventId={EventId}", evt.EventId);
                        break;
                    }

                    var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<IssueAssignedEvent>>();
                    await handler.HandleAsync(evt);

                    await processedRepo.AddAsync(new ProcessedEvent(evt.EventId, eventType));
                    await unitOfWork.SaveChangesAsync(CancellationToken.None);
                    break;
                }
                default:
                    _logger.LogWarning("Unknown event type received: {EventType}", eventType);
                    break;
            }

            _channel!.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}", eventType);
            _channel!.BasicNack(args.DeliveryTag, multiple: false, requeue: false); // Goes to DLQ
        }
    }

    private void InitializeRabbit()
    {
        var host = _configuration["RabbitMQ:Host"] ?? "rabbitmq";
        var port = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672;
        var user = _configuration["RabbitMQ:Username"] ?? "admin";
        var pass = _configuration["RabbitMQ:Password"] ?? "admin123";
        var vhost = _configuration["RabbitMQ:VirtualHost"] ?? "/";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = pass,
            VirtualHost = vhost,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();
        return base.StopAsync(cancellationToken);
    }
}
