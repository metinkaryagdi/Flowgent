using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
// Both System.Net and Microsoft.AspNetCore.HttpOverrides define IPNetwork;
// ForwardedHeadersOptions.KnownNetworks expects the ASP.NET Core one.
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
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

    /// <summary>
    /// Configures X-Forwarded-For / X-Forwarded-Proto handling for running behind a
    /// reverse proxy (Caddy in docker-compose.prod.yml). Without this, every request
    /// appears to come from the proxy's IP -- which collapses the gateway's per-IP rate
    /// limiter into a single shared bucket -- and Request.Scheme stays "http".
    ///
    /// Only proxies inside the private container network are trusted, so external
    /// clients cannot spoof their address by sending their own X-Forwarded-For header.
    /// Override the trusted ranges with ForwardedHeaders__KnownNetworks
    /// (comma-separated CIDR, e.g. "10.0.0.0/8,172.16.0.0/12").
    /// </summary>
    public static IServiceCollection AddReverseProxyForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configured = configuration["ForwardedHeaders:KnownNetworks"];
        var cidrs = string.IsNullOrWhiteSpace(configured)
            ? ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16", "127.0.0.0/8"]
            : configured.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Defaults contain only loopback; replace with the trusted proxy ranges.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var cidr in cidrs)
            {
                var parts = cidr.Split('/');
                if (parts.Length == 2
                    && IPAddress.TryParse(parts[0], out var prefix)
                    && int.TryParse(parts[1], out var prefixLength))
                {
                    options.KnownNetworks.Add(new IPNetwork(prefix, prefixLength));
                }
                else if (IPAddress.TryParse(cidr, out var single))
                {
                    options.KnownProxies.Add(single);
                }
            }

            // One hop: client -> Caddy -> service. Raise only if more proxies are added.
            options.ForwardLimit = configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 1;
        });

        return services;
    }
}
