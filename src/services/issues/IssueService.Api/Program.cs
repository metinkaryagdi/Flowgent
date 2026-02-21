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

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddScoped<IEventHandler<IssueAddedToSprintEvent>, IssueAddedToSprintEventHandler>();
builder.Services.AddScoped<IEventHandler<IssueRemovedFromSprintEvent>, IssueRemovedFromSprintEventHandler>();
builder.Services.AddHostedService<SprintEventsConsumer>();

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
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
});

builder.Services.AddIssueApplication();
builder.Services.AddIssueInfrastructure(builder.Configuration);
builder.Services.AddRabbitMQ(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCorrelationId();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IssueDbContext>();
    db.Database.Migrate();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
