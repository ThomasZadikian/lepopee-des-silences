using FluentValidation;
using Leds.Player.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Leds.Player.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddPlayerApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
