using Leds.Player.Application.Abstractions;
using Leds.Player.Infrastructure.Persistence;
using Leds.Player.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Leds.Player.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPlayerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PlayerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PlayerDb"))
                   .EnableSensitiveDataLogging()
                   .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information));

        services.AddScoped<IPlayerProfileRepository, EfPlayerProfileRepository>();
        services.AddScoped<IProcessedIntegrationEventRepository, EfProcessedIntegrationEventRepository>();
        services.AddScoped<PlayerSeedRunner>();

        return services;
    }
}
