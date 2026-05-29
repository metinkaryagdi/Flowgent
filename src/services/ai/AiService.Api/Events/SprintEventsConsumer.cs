using System.Text;
using System.Text.Json;
using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.Features.Sprints.Commands.GenerateRetrospective;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Common.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.AiService.Api.Events;

public sealed class SprintEventsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SprintEventsConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = RabbitMqTopology.EventsExchangeName;
    private const string ServiceName = "AiService";

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
        try
        {
            InitializeRabbit();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ connection failed for SprintEventsConsumer. Retro generation will be unavailable.");
            return Task.CompletedTask;
        }

        _channel!.BasicQos(0, 5, false);

        var eventType = nameof(SprintCompletedEvent);
        var dlxName = RabbitMqTopology.DeadLetterExchangeName;

        _channel.ExchangeDeclare(exchange: dlxName, type: ExchangeType.Topic, durable: true, autoDelete: false);

        var dlqName = RabbitMqTopology.GetDeadLetterQueueName(ServiceName, eventType);
        _channel.QueueDeclare(queue: dlqName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: dlqName, exchange: dlxName,
            routingKey: RabbitMqTopology.GetDeadLetterRoutingKey(ServiceName, eventType));

        var queueName = RabbitMqTopology.GetQueueName(ServiceName, eventType);
        var queueArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = dlxName,
            ["x-dead-letter-routing-key"] = RabbitMqTopology.GetDeadLetterRoutingKey(ServiceName, eventType)
        };
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind(queue: queueName, exchange: ExchangeName, routingKey: eventType);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("AiService listening for {EventType} events", eventType);
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
        => HandleMessageInternalAsync(args);

    private async Task HandleMessageInternalAsync(BasicDeliverEventArgs args)
    {
        var eventType = args.BasicProperties?.Type ?? args.RoutingKey;
        var message = Encoding.UTF8.GetString(args.Body.ToArray());
        try
        {
            var evt = JsonSerializer.Deserialize<SprintCompletedEvent>(message);
            if (evt is null)
            {
                _logger.LogWarning("Received null SprintCompletedEvent payload");
                _channel!.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AiDbContext>();
            var processedRepo = scope.ServiceProvider.GetRequiredService<IProcessedEventRepository>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await using var tx = await dbContext.Database.BeginTransactionAsync();

            if (await processedRepo.ExistsAsync(evt.EventId))
            {
                _logger.LogInformation("Duplicate SprintCompletedEvent ignored. EventId={EventId}", evt.EventId);
                await tx.CommitAsync();
                _channel!.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            _logger.LogInformation("Generating retrospective for sprint {SprintId}", evt.SprintId);

            await mediator.Send(new GenerateRetrospectiveCommand(
                evt.SprintId,
                evt.ProjectId,
                evt.CompletedByUserId,
                Guid.Empty));  // org_id not in event; session still saved

            await processedRepo.AddAsync(new ProcessedEvent(evt.EventId, eventType));
            await processedRepo.SaveChangesAsync(CancellationToken.None);
            await tx.CommitAsync();
            _channel!.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SprintCompletedEvent");
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
