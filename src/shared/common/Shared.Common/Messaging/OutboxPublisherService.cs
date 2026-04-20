using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Common.Health;

namespace Shared.Common.Messaging;

/// <summary>
/// Background service that publishes pending outbox messages to the event bus.
/// Uses optimistic claim/lock to prevent multi-instance duplicate publishing.
/// </summary>
public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly OutboxPublisherMonitor _monitor;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _lockDuration = TimeSpan.FromSeconds(30);
    private readonly Guid _workerId = Guid.NewGuid();
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    public OutboxPublisherService(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherService> logger,
        OutboxPublisherMonitor monitor)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _monitor = monitor;
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
                _monitor.RecordStarted(DateTime.UtcNow);
                var result = await ProcessOutboxMessagesAsync(stoppingToken);
                _monitor.RecordCompleted(DateTime.UtcNow, result.ProcessedCount, result.FailedCount, result.PermanentlyFailedCount);
            }
            catch (Exception ex)
            {
                _monitor.RecordFailure(DateTime.UtcNow, ex);
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

    private async Task<OutboxCycleResult> ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var outboxRepository = scope.ServiceProvider.GetService<IOutboxRepository>();
        var eventBus = scope.ServiceProvider.GetService<IEventBus>();

        if (outboxRepository == null || eventBus == null)
        {
            _logger.LogWarning("Outbox repository or EventBus not registered. Skipping outbox processing.");
            return new OutboxCycleResult(0, 0, 0);
        }

        // Claim a batch exclusively for this worker instance
        var messages = await outboxRepository.ClaimBatchAsync(_workerId, BatchSize, _lockDuration, cancellationToken);

        if (messages.Count == 0)
            return new OutboxCycleResult(0, 0, 0);

        _logger.LogInformation(
            "Processing {Count} outbox messages. WorkerId={WorkerId}",
            messages.Count, _workerId);

        var processedCount = 0;
        var failedCount = 0;
        var permanentlyFailedCount = 0;

        foreach (var message in messages)
        {
            IntegrationEventMetadataExtractor.TryExtract(message.Payload, out var metadata);

            try
            {
                await eventBus.PublishRawAsync(message.EventType, message.Payload, cancellationToken);

                await outboxRepository.MarkAsPublishedAsync(message.Id, DateTime.UtcNow, cancellationToken);
                processedCount += 1;

                _logger.LogDebug(
                    "Published outbox message. MessageId={MessageId}, EventType={EventType}, EventId={EventId}, EventVersion={EventVersion}, CorrelationId={CorrelationId}",
                    message.Id,
                    message.EventType,
                    metadata.EventId == Guid.Empty ? null : metadata.EventId,
                    metadata.EventVersion,
                    metadata.CorrelationId == Guid.Empty ? null : metadata.CorrelationId);
            }
            catch (Exception ex)
            {
                failedCount += 1;
                _logger.LogError(
                    ex,
                    "Failed to publish outbox message. MessageId={MessageId}, EventType={EventType}, EventId={EventId}, EventVersion={EventVersion}, RetryCount={RetryCount}",
                    message.Id,
                    message.EventType,
                    metadata.EventId == Guid.Empty ? null : metadata.EventId,
                    metadata.EventVersion,
                    message.RetryCount);

                // Exponential backoff: 10s, 30s, 60s, 120s, 300s
                var delaySeconds = Math.Min(10 * (int)Math.Pow(2, message.RetryCount), 300);
                var nextRetryAt = message.RetryCount < MaxRetries
                    ? DateTime.UtcNow.AddSeconds(delaySeconds)
                    : (DateTime?)null; // No more retries → permanently failed (DLQ)

                if (nextRetryAt is null)
                {
                    permanentlyFailedCount += 1;
                    _logger.LogCritical(
                        "Outbox message permanently failed after {MaxRetries} retries — manual intervention required. MessageId={MessageId}, EventType={EventType}",
                        MaxRetries, message.Id, message.EventType);
                }

                await outboxRepository.MarkAsFailedAsync(message.Id, ex.Message, nextRetryAt, cancellationToken);
            }
        }

        return new OutboxCycleResult(processedCount, failedCount, permanentlyFailedCount);
    }

    private readonly record struct OutboxCycleResult(int ProcessedCount, int FailedCount, int PermanentlyFailedCount);
}

