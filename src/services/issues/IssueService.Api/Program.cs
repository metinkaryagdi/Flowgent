using System.Text;
using BitirmeProject.IssueService.Api.Events;
using BitirmeProject.IssueService.Api.Events.Handlers;
using BitirmeProject.IssueService.Api.Middleware;
using BitirmeProject.IssueService.Application.DependencyInjection;
using BitirmeProject.IssueService.Infrastructure.DependencyInjection;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;
using Shared.Common.Extensions;
using Shared.Common.Health;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(Environment.GetEnvironmentVariable("Seq__ServerUrl") ?? "http://seq:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddScoped<IEventHandler<IssueAddedToSprintEvent>, IssueAddedToSprintEventHandler>();
builder.Services.AddScoped<IEventHandler<IssueRemovedFromSprintEvent>, IssueRemovedFromSprintEventHandler>();
builder.Services.AddHostedService<SprintEventsConsumer>();

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

var internalServiceApiKey = builder.Configuration["InternalService:ApiKey"];
if (builder.Environment.IsProduction() && (string.IsNullOrWhiteSpace(internalServiceApiKey) || internalServiceApiKey.Contains("change-me")))
    throw new InvalidOperationException("InternalService:ApiKey is set to the insecure default. Generate a strong random key and set InternalService__ApiKey (same value on every caller: issue, sprint, ai).");

builder.Services.AddAuthorization();
// Readiness dependencies: the service is only ready for traffic once its own
// database and the broker both answer. Liveness stays dependency-free so a
// database blip cannot trigger a restart storm across every replica.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<IssueDbContext>("database", tags: [HealthCheckExtensions.ReadyTag])
    .AddRabbitMqReadinessCheck();
builder.Services.AddReverseProxyForwardedHeaders(builder.Configuration);

// Redis (or in-memory fallback) is registered conditionally inside AddIssueInfrastructure
// based on the "Redis" connection string. Registering it unconditionally here as well would
// shadow that fallback (AddDistributedMemoryCache uses TryAdd, so it no-ops once an
// IDistributedCache is already present), which is why it is intentionally omitted.
builder.Services.AddIssueApplication();
builder.Services.AddIssueInfrastructure(builder.Configuration);
builder.Services.AddRabbitMQ(builder.Configuration);

// HttpClient for service-to-service calls
builder.Services.AddHttpClient("StorageService", client =>
{
    var baseUrl = builder.Configuration["Services:StorageService"] ?? "http://storage-service:8080";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

app.UseReverseProxyForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCorrelationId();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IssueDbContext>();
    if (app.Environment.IsEnvironment("Testing"))
        db.Database.EnsureCreated();
    else
    {
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(1)");
        try { await db.Database.MigrateAsync(); }
        finally { await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(1)"); }
    }
}

app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<InternalServiceMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthEndpoints();

app.Run();

public partial class Program { }
