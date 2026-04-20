using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<CorrelationContext>();

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
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();