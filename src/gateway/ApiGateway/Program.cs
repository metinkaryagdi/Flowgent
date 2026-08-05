using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.RateLimiting;
using ApiGateway;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Shared.Common.Extensions;
using Shared.Common.Health;
using Shared.Abstractions.Messaging;
using Yarp.ReverseProxy.Transforms;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(Environment.GetEnvironmentVariable("Seq__ServerUrl") ?? "http://seq:5341")
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
        var secret = builder.Configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret configuration is required (set Jwt__Secret env var).");
        if (builder.Environment.IsProduction() && secret.Contains("YourSuperSecret"))
            throw new InvalidOperationException("Jwt:Secret is set to the insecure default. Generate a strong 32+ char secret and set Jwt__Secret env var.");
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

                var services = context.HttpContext.RequestServices;
                var cancellationToken = context.HttpContext.RequestAborted;
                var cache = services.GetRequiredService<TokenStatusCache>();

                // A null verdict means "unknown", which covers both a plain cache miss and
                // an unreachable Redis -- either way the answer comes from IdentityService.
                var isValid = await cache.TryGetAsync(userId, securityStamp, cancellationToken);
                if (isValid is null)
                {
                    var client = services.GetRequiredService<IHttpClientFactory>()
                        .CreateClient("IdentityService");

                    try
                    {
                        var status = await client.GetFromJsonAsync<TokenStatusResponse>(
                            $"/api/v1/identity/token-status?userId={userId}&securityStamp={securityStamp}",
                            cancellationToken);

                        isValid = status?.IsValid == true;
                    }
                    catch
                    {
                        // Fail closed: this is the authority on whether the token is still
                        // good, and guessing "yes" would let revoked tokens through for as
                        // long as IdentityService is unhealthy.
                        context.Fail("Unable to validate token status.");
                        return;
                    }

                    await cache.SetAsync(userId, securityStamp, isValid.Value, cancellationToken);
                }

                if (!isValid.Value)
                    context.Fail("User is inactive or token has been invalidated.");
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddReverseProxyForwardedHeaders(builder.Configuration);
builder.Services.AddScoped<CorrelationContext>();

// Token-status caching has to be shared across gateway replicas, so it lives in Redis.
// Without a connection string the cache degrades to per-process memory, which is correct
// for a single instance and wrong the moment a second one starts -- hence the warning.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "gateway:";
    });
}
else
{
    if (builder.Environment.IsProduction())
    {
        Log.Warning(
            "ConnectionStrings:Redis is not configured. Token-status caching falls back to " +
            "per-process memory, so revocation takes effect per replica. Do not run more " +
            "than one gateway replica in this state.");
    }

    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddSingleton<TokenStatusCache>();
builder.Services.AddHttpClient("IdentityService", client =>
{
    var baseUrl = builder.Configuration["IdentityService:BaseUrl"] ?? "http://identity-api:8080";
    client.BaseAddress = new Uri(baseUrl);
});

// Per-IP, per-path rate limiter registered as a singleton.
//
// The paths below must stay in sync with IdentityService's AuthController, which is
// routed at "api/v1/identity" -- there is no "/auth/" segment anywhere in it. An earlier
// version matched on "/auth/login" and friends, so every rule here silently matched
// nothing and the gateway rate-limited absolutely nothing. Verified against the running
// stack after the fix: the 11th login in a minute now yields a 429.
//
// Matching is exact (not Contains) and case-folded: routing treats paths
// case-insensitively, so "/API/V1/Identity/Login" reaches the same action and must land
// in the same partition rather than slipping through as unlimited.
var partitionedLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
{
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var path = (context.Request.Path.Value ?? string.Empty).TrimEnd('/').ToLowerInvariant();

    if (path is "/api/v1/identity/login" or "/api/v1/identity/refresh")
        return RateLimitPartition.GetFixedWindowLimiter($"auth_strict:{ip}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });

    // The invite-validation path carries the token as a route segment, hence the prefix
    // match; the others are complete paths.
    if (path is "/api/v1/identity/register"
        || path.StartsWith("/api/v1/identity/invites/validate/", StringComparison.Ordinal))
        return RateLimitPartition.GetFixedWindowLimiter($"auth_register:{ip}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });

    // Tighter than registration, because each accepted request sends an email to an
    // address the caller chooses. Without this, the endpoint is a free mail cannon: point
    // it at someone else's address and hit it in a loop.
    if (path is "/api/v1/identity/resend-verification")
        return RateLimitPartition.GetFixedWindowLimiter($"auth_resend:{ip}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 });

    // Redeeming a token sends no mail, so it does not need the mail-cannon budget -- and
    // it must not share one, or a burst of resends would lock the user out of the link
    // they just asked for. The limit here only exists to stop hammering; guessing a
    // 256-bit token is not a threat this number is defending against. Deliberately loose
    // enough to survive a shared NAT, a mail client that prefetches links, and a user who
    // reloads the page.
    if (path is "/api/v1/identity/verify-email")
        return RateLimitPartition.GetFixedWindowLimiter($"auth_verify:{ip}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 });

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

// Must precede the rate limiter below: without it every request carries the
// reverse proxy's IP and all clients share a single rate-limit partition.
app.UseReverseProxyForwardedHeaders();

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

app.MapHealthEndpoints();
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
