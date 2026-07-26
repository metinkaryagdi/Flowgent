using System.Text;
using BitirmeProject.Bff.Api.Handlers;
using BitirmeProject.Bff.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Shared.Abstractions.Messaging;
using Shared.Common.Extensions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(Environment.GetEnvironmentVariable("Seq__ServerUrl") ?? "http://seq:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers();

builder.Services.Configure<ServiceEndpoints>(builder.Configuration.GetSection("ServiceEndpoints"));

// --- YENİ RESILIENCE (DAYANIKLILIK) POLİTİKAMIZ ---
static IAsyncPolicy<HttpResponseMessage> GetResiliencePolicy()
{
    // 1. TIMEOUT (Zaman Aşımı): Bir HTTP isteği en fazla 200 milisaniye bekleyebilir.
    var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(200));

    // 2. CIRCUIT BREAKER (Devre Kesici): Peş peşe 5 başarısız (veya zaman aşımı) istek gelirse şalteri indir!
    // Şalter inince 5 saniye boyunca arkadaki servise hiç gitme, anında hata dön.
    var circuitBreakerPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<Polly.Timeout.TimeoutRejectedException>()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(5)
        );

    // 3. RETRY (Yeniden Deneme): Kaos anında sistemi boğmamak için 3 denemeyi 1 denemeye düşürdük.
    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<Polly.Timeout.TimeoutRejectedException>()
        .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromMilliseconds(50));

    // Kuralları dıştan içe doğru sarıyoruz:
    // Önce Timeout makası başa geçer -> İçinde Retry çalışır -> En içte Circuit Breaker şalteri korur.
    return Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
}
// --------------------------------------------------

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<OrganizationContextHandler>();

// Mikroservislere artık yeni "GetResiliencePolicy" zırhımızı bağlıyoruz:
builder.Services.AddHttpClient("ProjectService", (sp, client) =>
{
    var endpoints = sp.GetRequiredService<IOptions<ServiceEndpoints>>().Value;
    client.BaseAddress = new Uri(endpoints.ProjectService);
}).AddPolicyHandler(GetResiliencePolicy())
  .AddHttpMessageHandler<OrganizationContextHandler>();

builder.Services.AddHttpClient("IssueService", (sp, client) =>
{
    var endpoints = sp.GetRequiredService<IOptions<ServiceEndpoints>>().Value;
    client.BaseAddress = new Uri(endpoints.IssueService);
}).AddPolicyHandler(GetResiliencePolicy())
  .AddHttpMessageHandler<OrganizationContextHandler>();

builder.Services.AddHttpClient("SprintService", (sp, client) =>
{
    var endpoints = sp.GetRequiredService<IOptions<ServiceEndpoints>>().Value;
    client.BaseAddress = new Uri(endpoints.SprintService);
}).AddPolicyHandler(GetResiliencePolicy())
  .AddHttpMessageHandler<OrganizationContextHandler>();

builder.Services.AddHttpClient("NotificationService", (sp, client) =>
{
    var endpoints = sp.GetRequiredService<IOptions<ServiceEndpoints>>().Value;
    client.BaseAddress = new Uri(endpoints.NotificationService);
}).AddPolicyHandler(GetResiliencePolicy())
  .AddHttpMessageHandler<OrganizationContextHandler>();

// Seq loglama servisi iş mantığımızı kilitlememeli, ona eski basit retry kuralını bırakıyoruz:
static IAsyncPolicy<HttpResponseMessage> GetSimpleRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromMilliseconds(100));

builder.Services.AddHttpClient("Seq", (sp, client) =>
{
    var endpoints = sp.GetRequiredService<IOptions<ServiceEndpoints>>().Value;
    client.BaseAddress = new Uri(endpoints.Seq);
}).AddPolicyHandler(GetSimpleRetryPolicy());

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
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<CorrelationContext>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRouting();
app.UseCorrelationId();

// Read accessToken cookie and inject as Bearer token for JWT authentication
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("Authorization") &&
        context.Request.Cookies.TryGetValue("accessToken", out var token))
    {
        context.Request.Headers.Authorization = $"Bearer {token}";
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();