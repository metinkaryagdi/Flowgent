using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shared.Common.Health;

/// <summary>
/// Liveness/readiness split for the service health endpoints.
///
/// <c>/health/live</c> answers "is this process running" and deliberately runs no
/// dependency checks: a database outage must not make the orchestrator restart every
/// replica, which only adds load to a struggling dependency.
///
/// <c>/health/ready</c> runs every check tagged <see cref="ReadyTag"/> -- database and
/// broker connectivity -- and is what a load balancer should poll before sending
/// traffic. <c>/health</c> stays as the aggregate view for humans and dashboards.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>Tag applied to checks that gate readiness for traffic.</summary>
    public const string ReadyTag = "ready";

    /// <summary>
    /// Registers a RabbitMQ connectivity check tagged as a readiness dependency.
    /// Requires <c>RabbitMQOptions</c> to be configured (done by <c>AddRabbitMQ</c>).
    /// </summary>
    public static IHealthChecksBuilder AddRabbitMqReadinessCheck(this IHealthChecksBuilder builder)
        => builder.AddCheck<RabbitMqHealthCheck>(
            "rabbitmq",
            failureStatus: HealthStatus.Unhealthy,
            tags: [ReadyTag]);

    /// <summary>
    /// Maps <c>/health</c>, <c>/health/live</c>, and <c>/health/ready</c>.
    /// Responses are JSON so a failing dependency can be identified without shelling
    /// into the container.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Aggregate view -- every registered check.
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteJsonResponseAsync
        });

        // Liveness: no dependency checks at all, just "the process answers".
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteJsonResponseAsync
        });

        // Readiness: only the checks that gate serving traffic.
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
            ResponseWriter = WriteJsonResponseAsync
        });

        return endpoints;
    }

    private static Task WriteJsonResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds,
                error = entry.Value.Exception?.Message
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
