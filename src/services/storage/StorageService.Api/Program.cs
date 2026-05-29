using System.Text;
using BitirmeProject.StorageService.Api.Background;
using BitirmeProject.StorageService.Api.Health;
using BitirmeProject.StorageService.Api.Middleware;
using BitirmeProject.StorageService.Application.DependencyInjection;
using BitirmeProject.StorageService.Infrastructure.DependencyInjection;
using BitirmeProject.StorageService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Shared.Abstractions.Messaging;
using Shared.Common.Extensions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret configuration is required (set Jwt__Secret env var).");
        if (secret.Contains("YourSuperSecret"))
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
builder.Services.AddScoped<CorrelationContext>();

builder.Services.AddStorageApplication();
builder.Services.AddStorageInfrastructure(builder.Configuration);
builder.Services.AddSingleton<StorageCleanupMonitor>();
builder.Services.AddHostedService<StorageOrphanCleanupService>();
builder.Services
    .AddHealthChecks()
    .AddCheck<StorageCleanupHealthCheck>("storage_cleanup_worker");

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCorrelationId();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
    db.Database.Migrate();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
