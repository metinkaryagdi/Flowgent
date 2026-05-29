using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Infrastructure.DependencyInjection;
using BitirmeProject.IdentityService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
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
            },
            OnTokenValidated = async ctx =>
            {
                var stampClaim = ctx.Principal?.FindFirst("security_stamp")?.Value;
                var userIdStr = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (stampClaim is null || !Guid.TryParse(userIdStr, out var userId))
                {
                    ctx.Fail("Invalid token claims.");
                    return;
                }

                var repo = ctx.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = await repo.GetByIdAsync(userId);

                if (user is null || !user.IsActive)
                {
                    ctx.Fail("User not found or inactive.");
                    return;
                }

                if (user.SecurityStamp.ToString() != stampClaim)
                {
                    ctx.Fail("Security stamp mismatch. Token has been invalidated.");
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

// Uygulama & altyapı
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddRabbitMQ(builder.Configuration);

var app = builder.Build();

// Db migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    if (app.Environment.IsEnvironment("Testing"))
        db.Database.EnsureCreated();
    else
    {
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(1)");
        try { await db.Database.MigrateAsync(); }
        finally { await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(1)"); }
    }

    await IdentityRoleSeeder.SeedAsync(db);

    var passwordHasher = scope.ServiceProvider.GetRequiredService<BitirmeProject.IdentityService.Application.Abstractions.IPasswordHasher>();
    await AdminUserSeeder.SeedAsync(db, passwordHasher, app.Configuration);
}

app.UseMiddleware<BitirmeProject.IdentityService.Api.Middleware.ExceptionHandlingMiddleware>();
app.UseRouting();
app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
