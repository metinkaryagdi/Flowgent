using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;

namespace Shared.Common.Messaging;

/// <summary>
/// Background service that publishes pending outbox messages to RabbitMQ
/// Implements the Transactional Outbox Pattern
/// </summary>
public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

    public OutboxPublisherService(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Publisher Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Outbox Publisher Service stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var outboxRepository = scope.ServiceProvider.GetService<IOutboxRepository>();
        var eventBus = scope.ServiceProvider.GetService<IEventBus>();

        if (outboxRepository == null || eventBus == null)
        {
            _logger.LogWarning("Outbox repository or EventBus not registered. Skipping outbox processing.");
            return;
        }

        var messages = await outboxRepository.GetPendingMessagesAsync(50, cancellationToken);

        if (messages.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await eventBus.PublishRawAsync(message.EventType, message.Payload, cancellationToken);
                await outboxRepository.MarkAsPublishedAsync(message.Id, cancellationToken);

                _logger.LogDebug(
                    "Published outbox message {MessageId} of type {EventType}",
                    message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Failed to publish outbox message {MessageId} of type {EventType}",
                    message.Id, message.EventType);

                await outboxRepository.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }
}
