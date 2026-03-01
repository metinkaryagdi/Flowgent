using Microsoft.AspNetCore.Builder;
using Shared.Common.Middleware;

namespace Shared.Common.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
