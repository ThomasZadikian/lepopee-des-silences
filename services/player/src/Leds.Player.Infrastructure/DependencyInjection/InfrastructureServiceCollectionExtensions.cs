using Leds.Player.Application.Abstractions;
using Leds.Player.Infrastructure.Persistence.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Leds.Player.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPlayerInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPlayerProfileRepository, InMemoryPlayerProfileRepository>();

        return services;
    }
}
