using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Infrastructure.Clock;
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
using Microsoft.Extensions.DependencyInjection;

namespace Leds.GameEngine.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddGameEngineInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRunRepository, InMemoryRunRepository>();

        /// Génération
        services.AddSingleton<ISeededRandomFactory, SeededRandomFactory>();

        services.AddSingleton<IRoomTypeResolver, RoomTypeResolver>();
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

        return services;
    }
}