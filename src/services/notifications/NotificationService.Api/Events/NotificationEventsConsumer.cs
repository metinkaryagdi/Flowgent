using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Domain.Entities;
using BitirmeProject.NotificationService.Infrastructure.Persistence;
using Shared.Abstractions.Messaging;
using Shared.Common.Logging;
using Shared.Common.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.NotificationService.Api.Events;

public sealed class NotificationEventsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationEventsConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = RabbitMqTopology.EventsExchangeName;
    private const string ServiceName = "NotificationService";
    private const string DlxName = RabbitMqTopology.DeadLetterExchangeName;

    public NotificationEventsConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationEventsConsumer> logger,
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
            nameof(IssueAssignedEvent),
            nameof(IssueStatusChangedEvent),
            nameof(CommentAddedEvent),
            nameof(MemberAddedEvent),
            nameof(UserInvitedEvent)
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
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            var processedRepo = scope.ServiceProvider.GetRequiredService<IProcessedEventRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await using var tx = await dbContext.Database.BeginTransactionAsync();
            switch (eventType)
            {
                case nameof(IssueAssignedEvent):
                {
                    var evt = JsonSerializer.Deserialize<IssueAssignedEvent>(message);
                    if (evt is null) throw new InvalidOperationException("Invalid IssueAssignedEvent payload");
                    using var logScope = _logger.BeginIntegrationEventScope(evt, $"{ServiceName}.{eventType}", evt.IssueId, evt.AssignedByUserId);

                    _logger.LogInformation("IssueAssignedEvent received.");

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
                case nameof(IssueStatusChangedEvent):
                {
                    var evt = JsonSerializer.Deserialize<IssueStatusChangedEvent>(message);
                    if (evt is null) throw new InvalidOperationException("Invalid IssueStatusChangedEvent payload");
                    using var logScope = _logger.BeginIntegrationEventScope(evt, $"{ServiceName}.{eventType}", evt.IssueId, evt.ChangedByUserId);

                    _logger.LogInformation("IssueStatusChangedEvent received.");

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
                case nameof(CommentAddedEvent):
                {
                    var evt = JsonSerializer.Deserialize<CommentAddedEvent>(message);
                    if (evt is null) throw new InvalidOperationException("Invalid CommentAddedEvent payload");
                    using var logScope = _logger.BeginIntegrationEventScope(evt, $"{ServiceName}.{eventType}", evt.IssueId, evt.AuthorUserId);

                    _logger.LogInformation("CommentAddedEvent received.");

                    if (await processedRepo.ExistsAsync(evt.EventId))
                    {
                        _logger.LogInformation("Duplicate CommentAddedEvent ignored. EventId={EventId}", evt.EventId);
                        break;
                    }

                    var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<CommentAddedEvent>>();
                    await handler.HandleAsync(evt);

                    await processedRepo.AddAsync(new ProcessedEvent(evt.EventId, eventType));
                    await unitOfWork.SaveChangesAsync(CancellationToken.None);
                    break;
                }
                case nameof(MemberAddedEvent):
                {
                    var evt = JsonSerializer.Deserialize<MemberAddedEvent>(message);
                    if (evt is null) throw new InvalidOperationException("Invalid MemberAddedEvent payload");
                    using var logScope = _logger.BeginIntegrationEventScope(evt, $"{ServiceName}.{eventType}", evt.ProjectId, evt.AddedByUserId);

                    _logger.LogInformation("MemberAddedEvent received.");

                    if (await processedRepo.ExistsAsync(evt.EventId))
                    {
                        _logger.LogInformation("Duplicate MemberAddedEvent ignored. EventId={EventId}", evt.EventId);
                        break;
                    }

                    var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<MemberAddedEvent>>();
                    await handler.HandleAsync(evt);

                    await processedRepo.AddAsync(new ProcessedEvent(evt.EventId, eventType));
                    await unitOfWork.SaveChangesAsync(CancellationToken.None);
                    break;
                }
                case nameof(UserInvitedEvent):
                {
                    var evt = JsonSerializer.Deserialize<UserInvitedEvent>(message);
                    if (evt is null) throw new InvalidOperationException("Invalid UserInvitedEvent payload");

                    _logger.LogInformation("UserInvitedEvent received. Email={Email}", evt.Email);

                    if (await processedRepo.ExistsAsync(evt.EventId))
                    {
                        _logger.LogInformation("Duplicate UserInvitedEvent ignored. EventId={EventId}", evt.EventId);
                        break;
                    }

                    var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<UserInvitedEvent>>();
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
