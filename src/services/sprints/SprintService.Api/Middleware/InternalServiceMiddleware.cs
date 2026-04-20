namespace BitirmeProject.SprintService.Api.Middleware;

public sealed class InternalServiceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;
    private readonly ILogger<InternalServiceMiddleware> _logger;
    private static readonly HashSet<string> AllowedCallers = ["AiService", "IssueService", "ProjectService", "BffService"];

    public InternalServiceMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<InternalServiceMiddleware> logger)
    {
        _next = next;
        _apiKey = configuration["InternalService:ApiKey"] ?? string.Empty;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var internalHeader = context.Request.Headers["X-Internal-Service"].FirstOrDefault();
        var apiKeyHeader = context.Request.Headers["X-Internal-Service-Key"].FirstOrDefault();
        var userIdHeader = context.Request.Headers["X-User-Id"].FirstOrDefault();
        var organizationIdHeader = context.Request.Headers["X-Organization-Id"].FirstOrDefault();
        var organizationRoleHeader = context.Request.Headers["X-Organization-Role"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(internalHeader))
        {
            var maskedKey = apiKeyHeader?.Length > 4
                ? apiKeyHeader[..4] + "****"
                : "****";

            var isValid = !string.IsNullOrWhiteSpace(_apiKey)
                && string.Equals(apiKeyHeader, _apiKey, StringComparison.Ordinal)
                && Guid.TryParse(userIdHeader, out _)
                && AllowedCallers.Contains(internalHeader);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Internal service call rejected. Caller={Caller} Key={MaskedKey} Path={Path}",
                    internalHeader, maskedKey, context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            if (Guid.TryParse(userIdHeader, out var userId))
            {
                var claims = new List<System.Security.Claims.Claim>
                {
                    new(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
                    new("internal_call", "true")
                };

                if (Guid.TryParse(organizationIdHeader, out var organizationId))
                    claims.Add(new("org_id", organizationId.ToString()));

                if (!string.IsNullOrWhiteSpace(organizationRoleHeader))
                    claims.Add(new("org_role", organizationRoleHeader));

                var identity = new System.Security.Claims.ClaimsIdentity(claims, "Internal");
                context.User = new System.Security.Claims.ClaimsPrincipal(identity);

                _logger.LogInformation(
                    "Internal service call accepted. Caller={Caller} UserId={UserId} Path={Path}",
                    internalHeader, userId, context.Request.Path);
            }
        }

        await _next(context);
    }
}
