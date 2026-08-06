using Leds.Catalog.Application.Enemies.Definitions.Ports;
using Leds.Catalog.Application.Enemies.Loot.Ports;
using Leds.Catalog.Application.Rewards.GenericLoot.Ports;
using Leds.Catalog.Application.Curses.Ports;
using Leds.Catalog.Application.EffectSets.Ports;
using Leds.Catalog.Application.Items.Definitions.Ports;
using Leds.Catalog.Application.Items.Ports;
using Leds.Catalog.Application.Npcs.Definitions.Ports;
using Leds.Catalog.Application.NpcReputationAffinities.Ports;
using Leds.Catalog.Application.PalaceLaws.Ports;
using Leds.Catalog.Application.RewardCursePools.Ports;
using Leds.Catalog.Application.RewardTemplates.Ports;
using Leds.Catalog.Application.RoomBosses.Ports;
using Leds.Catalog.Application.Skills.Definitions.Ports;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.ReadStores.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Leds.Catalog.Application.Rooms.Ports;
using Leds.Catalog.Application.RoomThemeAffinities.Ports;
using Leds.Catalog.Application.RoomTypes.Ports;
using Leds.Catalog.Application.Worlds.Ports;

namespace Leds.Catalog.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CatalogDb")));

        services.AddScoped<CatalogSeedRunner>();
        services.AddScoped<ExternalCatalogContentSeeder>();
        services.AddScoped<CatalogIntegrityValidator>();
        services.AddScoped<ISkillDefinitionReadStore, EfSkillDefinitionReadStore>();
        services.AddScoped<IEnemyDefinitionReadStore, EfEnemyDefinitionReadStore>();
        services.AddScoped<IItemTemplateReadStore, EfItemDefinitionReadStore>();
        services.AddScoped<IItemDefinitionReadStore, EfItemDefinitionReadStore>();
        services.AddScoped<IEffectSetReadStore, EfEffectSetReadStore>();
        services.AddScoped<IRewardTemplateReadStore, EfRewardTemplateReadStore>();
        services.AddScoped<IPalaceLawDefinitionReadStore, EfPalaceLawDefinitionReadStore>();
        services.AddScoped<ICurseDefinitionReadStore, EfCurseDefinitionReadStore>();
        services.AddScoped<INpcDefinitionReadStore, EfNpcDefinitionReadStore>();
        services.AddScoped<INpcReputationAffinityReadStore, EfNpcReputationAffinityReadStore>();
        services.AddScoped<IRoomDefinitionReadStore, EfRoomDefinitionReadStore>();
        services.AddScoped<IRoomTypeDefinitionReadStore, EfRoomTypeDefinitionReadStore>();
        services.AddScoped<IWorldDefinitionReadStore, EfWorldDefinitionReadStore>();
        services.AddScoped<IRoomThemeAffinityReadStore, EfRoomThemeAffinityReadStore>();
        services.AddScoped<IRewardCursePoolReadStore, EfRewardCursePoolReadStore>();
        services.AddScoped<IRoomBossDefinitionReadStore, EfRoomBossDefinitionReadStore>();
        services.AddScoped<IEnemyLootTableReadStore, EfEnemyLootTableReadStore>();
        services.AddScoped<IGenericLootPoolReadStore, EfGenericLootPoolReadStore>();

        return services;
    }
}
