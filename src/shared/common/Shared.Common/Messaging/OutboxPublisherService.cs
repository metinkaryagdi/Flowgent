using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;

namespace Shared.Common.Messaging;

/// <summary>
/// Background service that publishes pending outbox messages to the event bus.
/// Uses optimistic claim/lock to prevent multi-instance duplicate publishing.
/// </summary>
public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _lockDuration = TimeSpan.FromSeconds(30);
    private readonly Guid _workerId = Guid.NewGuid();
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    public OutboxPublisherService(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Publisher Service started. WorkerId={WorkerId}", _workerId);

        // Release any orphan claims from a previous crashed instance at startup
        await ReleaseOrphanClaimsAtStartupAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during outbox processing cycle. WorkerId={WorkerId}", _workerId);
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Outbox Publisher Service stopped. WorkerId={WorkerId}", _workerId);
    }

    private async Task ReleaseOrphanClaimsAtStartupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var outboxRepository = scope.ServiceProvider.GetService<IOutboxRepository>();
            if (outboxRepository != null)
                await outboxRepository.ReleaseOrphanClaimsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release orphan outbox claims at startup.");
        }
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

        // Claim a batch exclusively for this worker instance
        var messages = await outboxRepository.ClaimBatchAsync(_workerId, BatchSize, _lockDuration, cancellationToken);

        if (messages.Count == 0)
            return;

        _logger.LogInformation(
            "Processing {Count} outbox messages. WorkerId={WorkerId}",
            messages.Count, _workerId);

        foreach (var message in messages)
        {
            try
            {
                await eventBus.PublishRawAsync(message.EventType, message.Payload, cancellationToken);

                await outboxRepository.MarkAsPublishedAsync(message.Id, DateTime.UtcNow, cancellationToken);

                _logger.LogDebug(
                    "Published outbox message. MessageId={MessageId}, EventType={EventType}, CorrelationId={CorrelationId}",
                    message.Id, message.EventType, message.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish outbox message. MessageId={MessageId}, EventType={EventType}, RetryCount={RetryCount}",
                    message.Id, message.EventType, message.RetryCount);

                // Exponential backoff: 10s, 30s, 60s, 120s, 300s
                var delaySeconds = Math.Min(10 * (int)Math.Pow(2, message.RetryCount), 300);
                var nextRetryAt = message.RetryCount < MaxRetries
                    ? DateTime.UtcNow.AddSeconds(delaySeconds)
                    : (DateTime?)null; // No more retries → stays Failed

                await outboxRepository.MarkAsFailedAsync(message.Id, ex.Message, nextRetryAt, cancellationToken);
            }
        }
    }
}

