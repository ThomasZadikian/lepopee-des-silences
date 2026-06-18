using Leds.Catalog.Application.Enemies.Definitions.Ports;
using Leds.Catalog.Application.Enemies.Ports;
using Leds.Catalog.Application.EventTemplates.Ports;
using Leds.Catalog.Application.Items.Ports;
using Leds.Catalog.Application.Npcs.Definitions.Ports;
using Leds.Catalog.Application.PalaceLaws.Ports;
using Leds.Catalog.Application.RoomBosses.Ports;
using Leds.Catalog.Application.Skills.Definitions.Ports;
using Leds.Catalog.Application.Skills.Ports;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.ReadStores;
using Leds.Catalog.Infrastructure.ReadStores.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Leds.Catalog.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var persistenceMode = configuration["Persistence:Mode"];

        if (string.Equals(persistenceMode, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<CatalogDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("CatalogDb")));

            services.AddScoped<CatalogSeedRunner>();
            services.AddScoped<ISkillDefinitionReadStore, EfSkillDefinitionReadStore>();
            services.AddScoped<IEnemyDefinitionReadStore, EfEnemyDefinitionReadStore>();
            services.AddScoped<IItemTemplateReadStore, EfItemDefinitionReadStore>();
            services.AddScoped<IPalaceLawDefinitionReadStore, EfPalaceLawDefinitionReadStore>();
            services.AddScoped<INpcDefinitionReadStore, EfNpcDefinitionReadStore>();
        }
        else
        {
            services.AddSingleton<ISkillDefinitionReadStore, InMemorySkillDefinitionReadStore>();
            services.AddSingleton<IEnemyDefinitionReadStore, InMemoryEnemyDefinitionReadStore>();
            services.AddSingleton<IItemTemplateReadStore, InMemoryItemTemplateReadStore>();
            services.AddSingleton<IPalaceLawDefinitionReadStore, InMemoryPalaceLawDefinitionReadStore>();
            services.AddSingleton<INpcDefinitionReadStore, InMemoryNpcDefinitionReadStore>();
        }

        services.AddSingleton<IEnemyTemplateReadStore, InMemoryEnemyTemplateReadStore>();
        services.AddSingleton<ISkillTemplateReadStore, InMemorySkillTemplateReadStore>();
        services.AddSingleton<IEventTemplateReadStore, InMemoryEventTemplateReadStore>();
        services.AddSingleton<IRoomBossDefinitionReadStore, InMemoryRoomBossDefinitionReadStore>();

        return services;
    }
}
