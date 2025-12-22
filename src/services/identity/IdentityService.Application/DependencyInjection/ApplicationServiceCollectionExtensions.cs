using System.Reflection;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using BitirmeProject.IdentityService.Application.Common.Behaviors;
using BitirmeProject.IdentityService.Application.Common.Mappings;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        var assembly = typeof(IdentityMappingProfile).Assembly;

        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // AutoMapper  ✅ BURASI DÜZELDİ
        services.AddAutoMapper(cfg => { }, assembly);

        // FluentValidation (validatorları assembly'den tara) ✅ BURASI DA DEĞİŞTİ
        services.AddValidatorsFromAssembly(assembly);

        // Validation pipeline behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
