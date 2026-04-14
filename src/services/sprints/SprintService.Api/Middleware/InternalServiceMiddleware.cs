namespace BitirmeProject.SprintService.Api.Middleware;

/// <summary>
/// Allows internal service-to-service calls (e.g. from AiService) to bypass JWT authentication.
/// The caller must supply X-Internal-Service and X-User-Id headers.
/// </summary>
public sealed class InternalServiceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;

    public InternalServiceMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiKey = configuration["InternalService:ApiKey"] ?? string.Empty;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var internalHeader = context.Request.Headers["X-Internal-Service"].FirstOrDefault();
        var apiKeyHeader = context.Request.Headers["X-Internal-Service-Key"].FirstOrDefault();
        var userIdHeader = context.Request.Headers["X-User-Id"].FirstOrDefault();
        var organizationIdHeader = context.Request.Headers["X-Organization-Id"].FirstOrDefault();
        var organizationRoleHeader = context.Request.Headers["X-Organization-Role"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(internalHeader)
            && !string.IsNullOrWhiteSpace(_apiKey)
            && string.Equals(apiKeyHeader, _apiKey, StringComparison.Ordinal)
            && Guid.TryParse(userIdHeader, out var userId))
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
                new System.Security.Claims.Claim("internal_call", "true")
            };

            if (Guid.TryParse(organizationIdHeader, out var organizationId))
                claims.Add(new System.Security.Claims.Claim("org_id", organizationId.ToString()));

            if (!string.IsNullOrWhiteSpace(organizationRoleHeader))
                claims.Add(new System.Security.Claims.Claim("org_role", organizationRoleHeader));

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Internal");
            context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}
