using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Common.Options;

namespace Shared.Common.Health;

/// <summary>
/// Readiness probe for the RabbitMQ broker.
///
/// Opens a short-lived connection rather than reusing the event bus connection, so a
/// broker that is reachable but refusing new connections (credentials rotated, vhost
/// removed, connection limit reached) is still reported as unhealthy. The check is
/// tagged "ready" -- a broker outage must drain the instance from the load balancer,
/// but it must not make the orchestrator kill an otherwise fine process.
/// </summary>
public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMQOptions _options;

    public RabbitMqHealthCheck(IOptions<RabbitMQOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object?>
        {
            ["host"] = _options.Host,
            ["port"] = _options.Port,
            ["virtualHost"] = _options.VirtualHost
        };

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3),
                SocketReadTimeout = TimeSpan.FromSeconds(3),
                SocketWriteTimeout = TimeSpan.FromSeconds(3)
            };

            using var connection = factory.CreateConnection("healthcheck");

            return Task.FromResult(connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ connection is open.", data)
                : HealthCheckResult.Unhealthy("RabbitMQ connection closed immediately after opening.", data: data));
        }
        catch (Exception ex)
        {
            data["error"] = ex.Message;
            return Task.FromResult(HealthCheckResult.Unhealthy("Cannot connect to RabbitMQ.", ex, data));
        }
    }
}
