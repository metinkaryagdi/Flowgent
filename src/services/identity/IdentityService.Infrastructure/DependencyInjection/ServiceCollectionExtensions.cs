using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Infrastructure.Persistence;
using BitirmeProject.IdentityService.Infrastructure.Repositories;
using BitirmeProject.IdentityService.Infrastructure.Services;
using BitirmeProject.IdentityService.Application.Options;
using BitirmeProject.IdentityService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;

namespace BitirmeProject.IdentityService.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Connection string ismini appsettings'te sen belirle
        var connectionString = configuration.GetConnectionString("IdentityDatabase");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        // IUnitOfWork â†’ DbContext
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IInviteRepository, InviteRepository>();

        // Email
        services.AddScoped<IEmailService, EmailService>();

        // Password hasher
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Outbox
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        return services;
    }
}
