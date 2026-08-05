using System.Text;
using BitirmeProject.NotificationService.Api.Background;
using BitirmeProject.NotificationService.Api.Events;
using BitirmeProject.NotificationService.Api.Events.Handlers;
using BitirmeProject.NotificationService.Api.Health;
using BitirmeProject.NotificationService.Api.Hubs;
using BitirmeProject.NotificationService.Api.Middleware;
using BitirmeProject.NotificationService.Application.DependencyInjection;
using BitirmeProject.NotificationService.Infrastructure.DependencyInjection;
using BitirmeProject.NotificationService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Shared.Abstractions.Messaging;
using Shared.Common.Extensions;
using Shared.Common.Health;
using Shared.Contracts.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(Environment.GetEnvironmentVariable("Seq__ServerUrl") ?? "http://seq:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers();

// SignalR keeps connection state in the process that owns the socket. With more than
// one notification-api replica behind the gateway, a notification published by the
// replica that consumed the event would never reach a client connected to a different
// replica. The Redis backplane fans messages out across replicas.
// Single-instance runs without a Redis connection string fall back to in-memory.
var signalR = builder.Services.AddSignalR();
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    signalR.AddStackExchangeRedis(redisConnection, options =>
    {
        // Namespaced so the backplane cannot collide with the cache keys other
        // services keep in the same Redis instance.
        options.Configuration.ChannelPrefix =
            StackExchange.Redis.RedisChannel.Literal("flowgent:notifications");
    });
}
else if (builder.Environment.IsProduction())
{
    Log.Warning(
        "No Redis connection string configured. SignalR is running without a backplane, "
      + "so notifications will be lost if notification-api runs more than one replica.");
}

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173", "http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddScoped<IEventHandler<IssueAssignedEvent>, IssueAssignedEventHandler>();
builder.Services.AddScoped<IEventHandler<IssueStatusChangedEvent>, IssueStatusChangedEventHandler>();
builder.Services.AddScoped<IEventHandler<CommentAddedEvent>, CommentAddedEventHandler>();
builder.Services.AddScoped<IEventHandler<MemberAddedEvent>, MemberAddedEventHandler>();
builder.Services.AddScoped<IEventHandler<UserInvitedEvent>, UserInvitedEventHandler>();
// Consumed by this same service: the read receipt goes out through the broker so it
// reaches whichever replica holds the user's other tabs.
builder.Services.AddScoped<IEventHandler<NotificationReadEvent>, NotificationReadEventHandler>();
builder.Services.AddHostedService<NotificationEventsConsumer>();
builder.Services.AddHostedService<NotificationDeliveryWorker>();
builder.Services.AddSingleton<NotificationDeliveryMonitor>();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
        options.MapInboundClaims = true;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                ctx.Token = ctx.Request.Cookies["accessToken"];

                if (string.IsNullOrWhiteSpace(ctx.Token)
                    && ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    var value = authHeader.ToString();
                    if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        ctx.Token = value["Bearer ".Length..].Trim();
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
// Readiness dependencies: the service is only ready for traffic once its own
// database and the broker both answer. Liveness stays dependency-free so a
// database blip cannot trigger a restart storm across every replica.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotificationDbContext>("database", tags: [HealthCheckExtensions.ReadyTag])
    .AddRabbitMqReadinessCheck();
builder.Services.AddReverseProxyForwardedHeaders(builder.Configuration);
builder.Services
    .AddHealthChecks()
    .AddCheck<NotificationDeliveryHealthCheck>("notification_delivery_worker")
    .AddCheck<NotificationDlqHealthCheck>("notification_dlq");

builder.Services.AddNotificationApplication();
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.Services.AddRabbitMQ(builder.Configuration);

var app = builder.Build();

app.UseReverseProxyForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseCorrelationId();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(1)");
    try { await db.Database.MigrateAsync(); }
    finally { await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(1)"); }
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");
app.MapHealthEndpoints();

app.Run();
