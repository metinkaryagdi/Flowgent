using Microsoft.AspNetCore.Builder;
using Shared.Common.Middleware;

namespace Shared.Common.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Applies the forwarded-header configuration from
    /// <see cref="ServiceCollectionExtensions.AddReverseProxyForwardedHeaders"/>.
    /// Must run before authentication, rate limiting, or anything else that reads
    /// the client IP or request scheme.
    /// </summary>
    public static IApplicationBuilder UseReverseProxyForwardedHeaders(this IApplicationBuilder app)
    {
        return app.UseForwardedHeaders();
    }
}
