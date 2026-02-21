using System.Text;
using BitirmeProject.Bff.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
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

builder.Services.AddControllers();

builder.Services.Configure<ServiceEndpoints>(builder.Configuration.GetSection("ServiceEndpoints"));

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt));
}

builder.Services.AddHttpClient("ProjectService", (sp, client) =>
{
    var endpoints = sp.GetRequiredService<IOptions<ServiceEndpoints>>().Value;
    client.BaseAddress = new Uri(endpoints.ProjectService);
}).AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient("IssueService", (sp, client) =>
{
    var endpoints = sp.GetRequiredService<IOptions<ServiceEndpoints>>().Value;
    client.BaseAddress = new Uri(endpoints.IssueService);
}).AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient("SprintService", (sp, client) =>
{
    var endpoints = sp.GetRequiredService<IOptions<ServiceEndpoints>>().Value;
    client.BaseAddress = new Uri(endpoints.SprintService);
}).AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient("NotificationService", (sp, client) =>
{
    var endpoints = sp.GetRequiredService<IOptions<ServiceEndpoints>>().Value;
    client.BaseAddress = new Uri(endpoints.NotificationService);
}).AddPolicyHandler(GetRetryPolicy());

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

var app = builder.Build();

app.UseRouting();
app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
