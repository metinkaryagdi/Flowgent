using System.Text;
using System.Text.Json;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Abstractions.Messaging;
using Shared.Common.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IssueService.Api.Events;

public sealed class SprintEventsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SprintEventsConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = RabbitMqTopology.EventsExchangeName;
    private const string ServiceName = "IssueService";
    private const string DlxName = RabbitMqTopology.DeadLetterExchangeName;

    public SprintEventsConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<SprintEventsConsumer> logger,
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
            nameof(IssueAddedToSprintEvent),
            nameof(IssueRemovedFromSprintEvent)
        };

        // Declare Dead Letter Exchange for failed messages
        _channel.ExchangeDeclare(exchange: DlxName, type: ExchangeType.Topic, durable: true, autoDelete: false);

        foreach (var eventType in eventTypes)
        {
            var dlqName = RabbitMqTopology.GetDeadLetterQueueName(ServiceName, eventType);
            _channel.QueueDeclare(queue: dlqName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(
                queue: dlqName,
                exchange: DlxName,
                routingKey: RabbitMqTopology.GetDeadLetterRoutingKey(ServiceName, eventType));

            var queueName = RabbitMqTopology.GetQueueName(ServiceName, eventType);
            var queueArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = DlxName,
                ["x-dead-letter-routing-key"] = RabbitMqTopology.GetDeadLetterRoutingKey(ServiceName, eventType)
            };
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
            _channel.QueueBind(queue: queueName, exchange: ExchangeName, routingKey: eventType);
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;

        foreach (var eventType in eventTypes)
        {
            var queueName = RabbitMqTopology.GetQueueName(ServiceName, eventType);
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
            var dbContext = scope.ServiceProvider.GetRequiredService<IssueDbContext>();
            var processedRepo = scope.ServiceProvider.GetRequiredService<IProcessedEventRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await using var tx = await dbContext.Database.BeginTransactionAsync();
            switch (eventType)
            {
                case nameof(IssueAddedToSprintEvent):
                {
                    var evt = JsonSerializer.Deserialize<IssueAddedToSprintEvent>(message);
                    if (evt is null) throw new InvalidOperationException("Invalid IssueAddedToSprintEvent payload");

                    if (await processedRepo.ExistsAsync(evt.EventId))
                    {
                        _logger.LogInformation("Duplicate IssueAddedToSprintEvent ignored. EventId={EventId}", evt.EventId);
                        break;
                    }

                    var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<IssueAddedToSprintEvent>>();
                    await handler.HandleAsync(evt);

                    await processedRepo.AddAsync(new ProcessedEvent(evt.EventId, eventType));
                    await unitOfWork.SaveChangesAsync(CancellationToken.None);
                    break;
                }
                case nameof(IssueRemovedFromSprintEvent):
                {
                    var evt = JsonSerializer.Deserialize<IssueRemovedFromSprintEvent>(message);
                    if (evt is null) throw new InvalidOperationException("Invalid IssueRemovedFromSprintEvent payload");

                    if (await processedRepo.ExistsAsync(evt.EventId))
                    {
                        _logger.LogInformation("Duplicate IssueRemovedFromSprintEvent ignored. EventId={EventId}", evt.EventId);
                        break;
                    }

                    var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<IssueRemovedFromSprintEvent>>();
                    await handler.HandleAsync(evt);

                    await processedRepo.AddAsync(new ProcessedEvent(evt.EventId, eventType));
                    await unitOfWork.SaveChangesAsync(CancellationToken.None);
                    break;
                }
                default:
                    _logger.LogWarning("Unknown event type received: {EventType}", eventType);
                    break;
            }

            await tx.CommitAsync();
            _channel!.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}", eventType);
            _channel!.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
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
