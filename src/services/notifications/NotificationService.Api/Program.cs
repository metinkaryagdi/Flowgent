using System.Text;
using BitirmeProject.NotificationService.Api.Events;
using BitirmeProject.NotificationService.Api.Events.Handlers;
using BitirmeProject.NotificationService.Api.Hubs;
using BitirmeProject.NotificationService.Api.Middleware;
using BitirmeProject.NotificationService.Api.Models;
using BitirmeProject.NotificationService.Application.DependencyInjection;
using BitirmeProject.NotificationService.Infrastructure.DependencyInjection;
using BitirmeProject.NotificationService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Shared.Abstractions.Messaging;
using Shared.Common.Extensions;
using Shared.Contracts.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.Configure<ServiceEndpoints>(builder.Configuration.GetSection("ServiceEndpoints"));
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt));
}

builder.Services.AddHttpClient("IssueService", (sp, client) =>
{
    var endpoints = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServiceEndpoints>>().Value;
    client.BaseAddress = new Uri(endpoints.IssueService);
}).AddPolicyHandler(GetRetryPolicy());

builder.Services.AddScoped<IEventHandler<NotificationRequestedEvent>, NotificationRequestedEventHandler>();
builder.Services.AddScoped<IEventHandler<IssueAssignedEvent>, IssueAssignedEventHandler>();
builder.Services.AddScoped<IEventHandler<CommentAddedEvent>, CommentAddedEventHandler>();
builder.Services.AddScoped<IEventHandler<MemberAddedEvent>, MemberAddedEventHandler>();
builder.Services.AddHostedService<NotificationEventsConsumer>();

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

builder.Services.AddNotificationApplication();
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.Services.AddRabbitMQ(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCorrelationId();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.Run();
