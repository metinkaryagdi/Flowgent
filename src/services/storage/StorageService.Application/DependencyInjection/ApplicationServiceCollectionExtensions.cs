using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using BitirmeProject.StorageService.Application.Common.Mappings;
using BitirmeProject.StorageService.Application.Common.Behaviors;

namespace BitirmeProject.StorageService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddStorageApplication(this IServiceCollection services)
    {
        var assembly = typeof(StorageMappingProfile).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddAutoMapper(cfg => { }, assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
