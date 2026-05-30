using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Infrastructure.Clock;
using Leds.GameEngine.Infrastructure.Generation;
using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Leds.GameEngine.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddGameEngineInfrastructure(this IServiceCollection services)
    {
        // Doit conserver les runs en memoire. 
        // TODO : Passer en Scoped 
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRunGenerator, DeterministicRunGenerator>();
        services.AddSingleton<IRunRepository, InMemoryRunRepository>();

        return services;
    }
}