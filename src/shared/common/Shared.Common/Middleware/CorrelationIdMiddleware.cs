using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Context;
using Shared.Abstractions.Messaging;

namespace Shared.Common.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        // Resolve ActorId from JWT Claims — never from request body
        var actorId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? context.User.FindFirstValue("sub");

        // Populate the CorrelationContext available to all services via DI
        var correlationContext = context.RequestServices.GetRequiredService<CorrelationContext>();
        correlationContext.CorrelationId = correlationId;
        correlationContext.ActorId = actorId;

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        Activity.Current?.SetTag("correlation_id", correlationId);
        Activity.Current?.SetTag("actor_id", actorId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("ActorId", actorId ?? "anonymous"))
        {
            await _next(context);
        }
    }
}
