using Leds.Catalog.Application.Enemies.Definitions.Ports;
using Leds.Catalog.Application.Enemies.Ports;
using Leds.Catalog.Application.EventTemplates.Ports;
using Leds.Catalog.Application.Items.Ports;
using Leds.Catalog.Application.PalaceLaws.Ports;
using Leds.Catalog.Application.RoomBosses.Ports;
using Leds.Catalog.Application.Skills.Ports;
using Leds.Catalog.Infrastructure.ReadStores;
using Microsoft.Extensions.DependencyInjection;

namespace Leds.Catalog.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<IEnemyTemplateReadStore, InMemoryEnemyTemplateReadStore>();
        services.AddSingleton<ISkillTemplateReadStore, InMemorySkillTemplateReadStore>();
        services.AddSingleton<IItemTemplateReadStore, InMemoryItemTemplateReadStore>();
        services.AddSingleton<IPalaceLawDefinitionReadStore, InMemoryPalaceLawDefinitionReadStore>();
        services.AddSingleton<IEventTemplateReadStore, InMemoryEventTemplateReadStore>();
        services.AddSingleton<IRoomBossDefinitionReadStore, InMemoryRoomBossDefinitionReadStore>();
        services.AddSingleton<IEnemyDefinitionReadStore, InMemoryEnemyDefinitionReadStore>();

        return services;
    }
}