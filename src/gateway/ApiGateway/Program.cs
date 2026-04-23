using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Shared.Common.Extensions;
using Shared.Abstractions.Messaging;
using Yarp.ReverseProxy.Transforms;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("gateway", policy =>
    {
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
        policy.AllowCredentials();
        policy.WithOrigins(allowedOrigins);
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["Jwt:Secret"] ?? "YourSuperSecretKeyForJwtTokenGenerationMinimum32Characters";
        var issuer = builder.Configuration["Jwt:Issuer"] ?? "BitirmeProject.IdentityService";
        var audience = builder.Configuration["Jwt:Audience"] ?? "BitirmeProject.Clients";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue("accessToken", out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? context.Principal?.FindFirstValue("sub");
                var stampValue = context.Principal?.FindFirstValue("security_stamp");

                if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(stampValue, out var securityStamp))
                {
                    context.Fail("Invalid token claims.");
                    return;
                }

                var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                var cacheKey = $"token-status:{userId}:{securityStamp}";
                if (!cache.TryGetValue(cacheKey, out bool isValid))
                {
                    var client = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>()
                        .CreateClient("IdentityService");

                    try
                    {
                        var status = await client.GetFromJsonAsync<TokenStatusResponse>(
                            $"/api/v1/identity/token-status?userId={userId}&securityStamp={securityStamp}",
                            context.HttpContext.RequestAborted);

                        isValid = status?.IsValid == true;
                    }
                    catch
                    {
                        context.Fail("Unable to validate token status.");
                        return;
                    }

                    cache.Set(
                        cacheKey,
                        isValid,
                        new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = isValid
                                ? TimeSpan.FromSeconds(10)
                                : TimeSpan.FromSeconds(2)
                        });
                }

                if (!isValid)
                    context.Fail("User is inactive or token has been invalidated.");
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<CorrelationContext>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("IdentityService", client =>
{
    var baseUrl = builder.Configuration["IdentityService:BaseUrl"] ?? "http://identity-api:8080";
    client.BaseAddress = new Uri(baseUrl);
});

// Per-IP, per-path rate limiter registered as a singleton
var partitionedLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
{
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var path = context.Request.Path.Value ?? string.Empty;

    if (path.Contains("/auth/login") || path.Contains("/auth/refresh"))
        return RateLimitPartition.GetFixedWindowLimiter($"auth_strict:{ip}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });

    if (path.Contains("/auth/register") || path.Contains("/invites/validate"))
        return RateLimitPartition.GetFixedWindowLimiter($"auth_register:{ip}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });

    return RateLimitPartition.GetNoLimiter("default");
});
builder.Services.AddSingleton(partitionedLimiter);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(ctx =>
    {
        ctx.AddRequestTransform(async transformCtx =>
        {
            var user = transformCtx.HttpContext.User;
            var orgId = user.FindFirst("org_id")?.Value;
            var orgRole = user.FindFirst("org_role")?.Value;

            transformCtx.ProxyRequest.Headers.Remove("X-Organization-Id");
            transformCtx.ProxyRequest.Headers.Remove("X-Organization-Role");

            if (!string.IsNullOrEmpty(orgId))
                transformCtx.ProxyRequest.Headers.Add("X-Organization-Id", orgId);

            if (!string.IsNullOrEmpty(orgRole))
                transformCtx.ProxyRequest.Headers.Add("X-Organization-Role", orgRole);

            await Task.CompletedTask;
        });
    });

var app = builder.Build();

app.UseCors("gateway");
app.UseCorrelationId();

// IP-based rate limiting for sensitive auth paths
app.Use(async (context, next) =>
{
    var limiter = context.RequestServices.GetRequiredService<PartitionedRateLimiter<HttpContext>>();
    using var lease = await limiter.AcquireAsync(context);
    if (!lease.IsAcquired)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers["Retry-After"] = "60";
        await context.Response.WriteAsync("Too many requests. Please try again later.");
        return;
    }
    await next(context);
});

app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (ShouldEnforceGatewayTokenStatus(context)
        && HasAccessToken(context)
        && context.User.Identity?.IsAuthenticated != true)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Authentication token is no longer valid.");
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();

static bool HasAccessToken(HttpContext context)
{
    if (context.Request.Cookies.ContainsKey("accessToken"))
        return true;

    return context.Request.Headers.TryGetValue("Authorization", out var value)
        && value.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
}

static bool ShouldEnforceGatewayTokenStatus(HttpContext context)
{
    var path = context.Request.Path;

    if (path.StartsWithSegments("/health"))
        return false;

    if (path.StartsWithSegments("/api/v1/identity/login")
        || path.StartsWithSegments("/api/v1/identity/register")
        || path.StartsWithSegments("/api/v1/identity/refresh")
        || path.StartsWithSegments("/api/v1/identity/invites/validate")
        || path.StartsWithSegments("/api/v1/identity/invites/accept"))
        return false;

    return true;
}

public sealed record TokenStatusResponse(bool IsValid);
