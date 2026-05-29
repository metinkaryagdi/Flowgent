using Shared.Contracts.Events;
using Shared.Abstractions.Messaging;
using BitirmeProject.ProjectService.Api.Events;
using BitirmeProject.ProjectService.Api.Events.Handlers;
using System.Text;
using System.Text.Json.Serialization;
using BitirmeProject.ProjectService.Api.Middleware;
using BitirmeProject.ProjectService.Application.DependencyInjection;
using BitirmeProject.ProjectService.Infrastructure.DependencyInjection;
using BitirmeProject.ProjectService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Shared.Common.Extensions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<IEventHandler<IssueCreatedEvent>, IssueCreatedEventHandler>();
builder.Services.AddScoped<IEventHandler<IssueStatusChangedEvent>, IssueStatusChangedEventHandler>();
builder.Services.AddScoped<IEventHandler<IssueAssignedEvent>, IssueAssignedEventHandler>();
builder.Services.AddHostedService<IssueEventsConsumer>();

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
builder.Services.AddHealthChecks();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
});

builder.Services.AddProjectApplication();
builder.Services.AddProjectInfrastructure(builder.Configuration);
builder.Services.AddRabbitMQ(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCorrelationId();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
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
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
