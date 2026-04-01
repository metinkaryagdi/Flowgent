using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Infrastructure.FileSystem;
using BitirmeProject.StorageService.Infrastructure.Persistence;
using BitirmeProject.StorageService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BitirmeProject.StorageService.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorageInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("StorageDatabase");
        var rootPath = configuration["Storage:RootPath"] ?? "storage-uploads";

        services.AddDbContext<StorageDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<StorageDbContext>());
        services.AddScoped<IStorageRepository, StorageRepository>();
        services.AddSingleton<IFileStorage>(_ => new FileSystemStorage(rootPath));

        return services;
    }
}
