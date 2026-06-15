using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.EncounterComposition;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Combats.Targeting;
using Leds.GameEngine.Application.Events.Ports;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Infrastructure.Catalog;
using Leds.GameEngine.Infrastructure.Clock;
using Leds.GameEngine.Infrastructure.Combats;
using Leds.GameEngine.Infrastructure.Combats.Actions;
using Leds.GameEngine.Infrastructure.Combats.EncounterComposition;
using Leds.GameEngine.Infrastructure.Combats.EncounterDrafts;
using Leds.GameEngine.Infrastructure.Combats.Targeting;
using Leds.GameEngine.Infrastructure.Events.Resolution;
using Leds.GameEngine.Infrastructure.Generation;
using Leds.GameEngine.Infrastructure.Generation.Randomness;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;
using Leds.GameEngine.Infrastructure.Outbox;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Repositories;
using Leds.GameEngine.Infrastructure.Players;
using Leds.GameEngine.Infrastructure.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leds.GameEngine.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddGameEngineInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRunRepository, InMemoryRunRepository>();

        RegisterPersistence(services, configuration);

        // Génération
        services.AddSingleton<ISeededRandomFactory, SeededRandomFactory>();

        services.AddSingleton<DeterministicMarkovSampler>();
        services.AddSingleton<MarkovTransitionResolver>();
        services.AddSingleton<IRoomTypeMarkovMatrixProvider, StaticRoomTypeMarkovMatrixProvider>();
        services.AddSingleton<IRoomTypeResolver, MarkovRoomTypeResolver>();
        services.AddSingleton<IRoomThemeResolver, RoomThemeResolver>();
        services.AddSingleton<IRoomBossProfileResolver, RoomBossProfileResolver>();

        services.AddSingleton<IRunGenerator, DeterministicRunGenerator>();

        services.AddSingleton<IRoomMapLayoutTemplateProvider, RoomMapLayoutTemplateProvider>();
        services.AddSingleton<IRoomTypeGenerationProfileProvider, HardcodedRoomTypeGenerationProfileProvider>();
        services.AddSingleton<IMapRoomGenerator, MapRoomGenerator>();
        RegisterCatalogGateway(services, configuration);
        RegisterPlayerGateway(services, configuration);
        RegisterOutbox(services, configuration);

        services.AddSingleton<IEventContentResolver, EventContentResolver>();

        services.AddSingleton<IEventContentResolutionStrategy, CombatEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, RoomBossEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, ItemEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, PalaceLawEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, NpcEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, RestEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, MerchantEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, RareEventContentResolutionStrategy>();

        services.AddSingleton<ICombatInstanceRepository, InMemoryCombatInstanceRepository>();
        services.AddSingleton<ICombatInstanceFactory, CombatInstanceFactory>();

        services.AddSingleton<ICombatTargetingRuleValidator, CombatTargetingRuleValidator>();
        services.AddSingleton<ICombatSkillActionValidator, CombatSkillActionValidator>();
        services.AddSingleton<ICombatSkillEffectResolver, CombatSkillEffectResolver>();
        services.AddSingleton<IEnemyCombatTurnResolver, EnemyCombatTurnResolver>();
        services.AddSingleton<IEncounterCompositionPolicy, EncounterCompositionPolicy>();
        services.AddSingleton<ICombatEncounterDraftGenerator, CombatEncounterDraftGenerator>();

        services.AddSingleton<IRewardOfferRepository, InMemoryRewardOfferRepository>();

        return services;
    }

    private static void RegisterPersistence(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var persistenceMode = configuration["Persistence:Mode"];

        if (string.Equals(persistenceMode, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<GameEngineDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("GameEngineDb")));

            services.AddScoped<IRunRepository, EfRunRepository>();
        }
    }

    private static void RegisterCatalogGateway(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CatalogGatewayOptions>(
            configuration.GetSection(CatalogGatewayOptions.SectionName));

        var options = configuration
            .GetSection(CatalogGatewayOptions.SectionName)
            .Get<CatalogGatewayOptions>() ?? new CatalogGatewayOptions();

        if (string.Equals(options.Mode, "Http", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ICatalogContentGateway, HttpCatalogContentGateway>(
                (serviceProvider, client) =>
                {
                    var gatewayOptions = serviceProvider
                        .GetRequiredService<IOptions<CatalogGatewayOptions>>()
                        .Value;

                    client.BaseAddress = new Uri(gatewayOptions.BaseUrl);
                    client.Timeout = gatewayOptions.Timeout;
                });
        }
        else
        {
            services.AddSingleton<ICatalogContentGateway, InMemoryCatalogContentGateway>();
        }
    }

    private static void RegisterPlayerGateway(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PlayerGatewayOptions>(
            configuration.GetSection(PlayerGatewayOptions.SectionName));

        var options = configuration
            .GetSection(PlayerGatewayOptions.SectionName)
            .Get<PlayerGatewayOptions>() ?? new PlayerGatewayOptions();

        if (string.Equals(options.Mode, "Http", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IPlayerRunSnapshotGateway, HttpPlayerRunSnapshotGateway>(
                (serviceProvider, client) =>
                {
                    var gatewayOptions = serviceProvider
                        .GetRequiredService<IOptions<PlayerGatewayOptions>>()
                        .Value;

                    client.BaseAddress = new Uri(gatewayOptions.BaseUrl);
                    client.Timeout = gatewayOptions.Timeout;
                });
        }
        else
        {
            services.AddSingleton<IPlayerRunSnapshotGateway, InMemoryPlayerRunSnapshotGateway>();
        }
    }

    private static void RegisterOutbox(IServiceCollection services, IConfiguration configuration)
    {
        var outboxEnabled = configuration.GetValue<bool>("Outbox:DispatcherEnabled", false);

        if (outboxEnabled)
        {
            services.AddHostedService<GameEngineOutboxDispatcherHostedService>();
        }
    }
}