using Leds.GameEngine.Domain.Markov;
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

namespace Leds.GameEngine.UnitTests.Common.Factories;

public static class TestGeneratorFactory
{
    public static DeterministicRunGenerator CreateDeterministicRunGenerator()
    {
        var candidateResolver = new NodeEventCandidateResolver();
        var eventGenerator = new NodeEventGenerator(
            candidateResolver,
            new MarkovNodeEventTypeResolver(
                new StaticNodeEventTypeMarkovMatrixProvider(),
                new MarkovTransitionResolver(new DeterministicMarkovSampler())));
        var riskResolver = new NodeRiskResolver();
        var rewardProfileResolver = new NodeRewardProfileResolver();

        var nodeFactory = new RoomNodeFactory(
            eventGenerator,
            riskResolver,
            rewardProfileResolver);

        var roomPlanGenerator = new RoomPlanGenerator(
            new RoomBossProfileResolver(),
            new RoomThemeResolver(),
            new RoomEventGenerationStateFactory(),
            new RoomNodeLayerPlanner(),
            nodeFactory);

        var roomTypeResolver = new MarkovRoomTypeResolver(
            new StaticRoomTypeMarkovMatrixProvider(),
            new MarkovTransitionResolver(new DeterministicMarkovSampler()));

        return new DeterministicRunGenerator(
            new SeededRandomFactory(),
            roomTypeResolver,
            roomPlanGenerator);
    }
}