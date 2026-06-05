using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Events.Ports;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Infrastructure.Catalog;
using Leds.GameEngine.Infrastructure.Clock;
using Leds.GameEngine.Infrastructure.Combats;
using Leds.GameEngine.Infrastructure.Events.Resolution;
using Leds.GameEngine.Infrastructure.Generation;
using Leds.GameEngine.Infrastructure.Generation.Randomness;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Events;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Layers;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Nodes;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Planning;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Rewards;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Risk;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Rewards;
using Microsoft.Extensions.DependencyInjection;

namespace Leds.GameEngine.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddGameEngineInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRunRepository, InMemoryRunRepository>();

        // Génération
        services.AddSingleton<ISeededRandomFactory, SeededRandomFactory>();

        services.AddSingleton<DeterministicMarkovSampler>();
        services.AddSingleton<MarkovTransitionResolver>();
        services.AddSingleton<INodeEventTypeMarkovMatrixProvider, StaticNodeEventTypeMarkovMatrixProvider>();
        services.AddSingleton<MarkovNodeEventTypeResolver>();
        services.AddSingleton<IRoomTypeMarkovMatrixProvider, StaticRoomTypeMarkovMatrixProvider>();
        services.AddSingleton<IRoomTypeResolver, MarkovRoomTypeResolver>();
        services.AddSingleton<IRoomThemeResolver, RoomThemeResolver>();
        services.AddSingleton<IRoomBossProfileResolver, RoomBossProfileResolver>();

        services.AddSingleton<IRoomEventGenerationStateFactory, RoomEventGenerationStateFactory>();
        services.AddSingleton<INodeEventCandidateResolver, NodeEventCandidateResolver>();
        services.AddSingleton<INodeEventGenerator, NodeEventGenerator>();

        services.AddSingleton<INodeRiskResolver, NodeRiskResolver>();
        services.AddSingleton<INodeRewardProfileResolver, NodeRewardProfileResolver>();

        services.AddSingleton<IRoomNodeFactory, RoomNodeFactory>();
        services.AddSingleton<IRoomNodeLayerPlanner, RoomNodeLayerPlanner>();

        services.AddSingleton<IRoomPlanGenerator, RoomPlanGenerator>();
        services.AddSingleton<IRunGenerator, DeterministicRunGenerator>();
        services.AddSingleton<ICatalogContentGateway, InMemoryCatalogContentGateway>();
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

        services.AddSingleton<IRewardOfferRepository, InMemoryRewardOfferRepository>();

        return services;
    }
}