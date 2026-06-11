using Leds.Player.Application.Abstractions;
using Leds.Player.Infrastructure.Persistence;
using Leds.Player.Infrastructure.Persistence.InMemory;
using Leds.Player.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Leds.Player.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPlayerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var persistenceMode = configuration["Persistence:Mode"];

        if (string.Equals(persistenceMode, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<PlayerDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("PlayerDb")));

            services.AddScoped<IPlayerProfileRepository, EfPlayerProfileRepository>();
            services.AddScoped<IProcessedIntegrationEventRepository, EfProcessedIntegrationEventRepository>();
        }
        else
        {
            services.AddSingleton<IPlayerProfileRepository, InMemoryPlayerProfileRepository>();
            services.AddSingleton<IProcessedIntegrationEventRepository, InMemoryProcessedIntegrationEventRepository>();
        }

        return services;
    }
}
