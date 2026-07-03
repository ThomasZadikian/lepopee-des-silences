using Leds.Catalog.Application.Enemies.Definitions.Ports;
using Leds.Catalog.Application.Enemies.Loot.Ports;
using Leds.Catalog.Application.Enemies.Ports;
using Leds.Catalog.Application.Rewards.GenericLoot.Ports;
using Leds.Catalog.Application.EventTemplates.Ports;
using Leds.Catalog.Application.Curses.Ports;
using Leds.Catalog.Application.EffectSets.Ports;
using Leds.Catalog.Application.Items.Definitions.Ports;
using Leds.Catalog.Application.Items.Ports;
using Leds.Catalog.Application.Npcs.Definitions.Ports;
using Leds.Catalog.Application.PalaceLaws.Ports;
using Leds.Catalog.Application.RewardCursePools.Ports;
using Leds.Catalog.Application.RewardTemplates.Ports;
using Leds.Catalog.Application.RoomBosses.Ports;
using Leds.Catalog.Application.Skills.Definitions.Ports;
using Leds.Catalog.Application.Skills.Ports;
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
        services.AddScoped<ISkillDefinitionReadStore, EfSkillDefinitionReadStore>();
        services.AddScoped<IEnemyDefinitionReadStore, EfEnemyDefinitionReadStore>();
        services.AddScoped<IItemTemplateReadStore, EfItemDefinitionReadStore>();
        services.AddScoped<IItemDefinitionReadStore, EfItemDefinitionReadStore>();
        services.AddScoped<IEffectSetReadStore, EfEffectSetReadStore>();
        services.AddScoped<IRewardTemplateReadStore, EfRewardTemplateReadStore>();
        services.AddScoped<IPalaceLawDefinitionReadStore, EfPalaceLawDefinitionReadStore>();
        services.AddScoped<ICurseDefinitionReadStore, EfCurseDefinitionReadStore>();
        services.AddScoped<INpcDefinitionReadStore, EfNpcDefinitionReadStore>();
        services.AddScoped<IRoomDefinitionReadStore, EfRoomDefinitionReadStore>();
        services.AddScoped<IRoomTypeDefinitionReadStore, EfRoomTypeDefinitionReadStore>();
        services.AddScoped<IWorldDefinitionReadStore, EfWorldDefinitionReadStore>();
        services.AddScoped<IRoomThemeAffinityReadStore, EfRoomThemeAffinityReadStore>();
        services.AddScoped<IRewardCursePoolReadStore, EfRewardCursePoolReadStore>();
        services.AddScoped<IEnemyTemplateReadStore, EfEnemyTemplateReadStore>();
        services.AddScoped<ISkillTemplateReadStore, EfSkillTemplateReadStore>();
        services.AddScoped<IEventTemplateReadStore, EfEventTemplateReadStore>();
        services.AddScoped<IRoomBossDefinitionReadStore, EfRoomBossDefinitionReadStore>();
        services.AddScoped<IEnemyLootTableReadStore, EfEnemyLootTableReadStore>();
        services.AddScoped<IGenericLootPoolReadStore, EfGenericLootPoolReadStore>();

        // Canon content seeders (git-ignored, local-only) — discovered by scan so the
        // build never depends on a concrete type that may be absent on a fresh clone.
        foreach (var canonType in typeof(CatalogDbContext).Assembly.GetTypes()
            .Where(t => typeof(Leds.Catalog.Infrastructure.Persistence.Canon.ICanonContentSeeder).IsAssignableFrom(t)
                        && t is { IsAbstract: false, IsInterface: false }))
        {
            services.AddScoped(typeof(Leds.Catalog.Infrastructure.Persistence.Canon.ICanonContentSeeder), canonType);
        }

        return services;
    }
}