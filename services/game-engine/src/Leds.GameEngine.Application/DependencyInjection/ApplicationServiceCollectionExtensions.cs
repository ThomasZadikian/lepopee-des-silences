using FluentValidation;
using Leds.GameEngine.Application.Common.Behaviors;
using Leds.GameEngine.Application.Runs.StartRun;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Leds.GameEngine.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddGameEngineApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(StartRunCommand).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(applicationAssembly);
        });

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}