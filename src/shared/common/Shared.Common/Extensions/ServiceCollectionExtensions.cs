using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Common.Health;
using Shared.Common.Messaging;
using Shared.Common.Options;

namespace Shared.Common.Extensions;

/// <summary>
/// Extension methods for registering shared services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers RabbitMQ event bus, outbox publisher, and correlation context.
    /// </summary>
    public static IServiceCollection AddRabbitMQ(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register options
        services.Configure<RabbitMQOptions>(
            configuration.GetSection(RabbitMQOptions.SectionName));

        // Register EventBus as singleton (shared connection)
        services.AddSingleton<IEventBus, RabbitMQEventBus>();
        services.AddSingleton<OutboxPublisherMonitor>();

        // Register Outbox Publisher background service
        services.AddHostedService<OutboxPublisherService>();
        services.AddHealthChecks().AddCheck<OutboxPublisherHealthCheck>("outbox_publisher_worker");

        // Register CorrelationContext as scoped — populated per request by CorrelationIdMiddleware
        services.AddScoped<CorrelationContext>();

        return services;
    }
}
