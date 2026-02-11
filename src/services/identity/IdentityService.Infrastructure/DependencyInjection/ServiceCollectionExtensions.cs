using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Infrastructure.Persistence;
using BitirmeProject.IdentityService.Infrastructure.Repositories;
using BitirmeProject.IdentityService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            options.UseNpgsql(connectionString);
        });

        // IUnitOfWork â†’ DbContext
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // Password hasher
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
