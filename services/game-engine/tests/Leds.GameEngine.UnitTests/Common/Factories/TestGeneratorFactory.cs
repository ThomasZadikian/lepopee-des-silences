using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Infrastructure.Catalog;
using Leds.GameEngine.Infrastructure.Generation;
using Leds.GameEngine.Infrastructure.Generation.Psyche;
using Leds.GameEngine.Infrastructure.Generation.Randomness;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.States;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;
using Leds.GameEngine.Infrastructure.Markov;

namespace Leds.GameEngine.UnitTests.Common.Factories;

public static class TestGeneratorFactory
{
    public static DeterministicRunGenerator CreateDeterministicRunGenerator()
    {
        var mapRoomGenerator = new MapRoomGenerator(
            new RoomMapLayoutTemplateProvider(),
            new RoomThemeResolver(),
            new RoomBossProfileResolver(new InMemoryCatalogContentGateway()),
            new HardcodedRoomTypeGenerationProfileProvider());

        var traceSink = new NullMarkovTransitionTraceSink();
        var calibration = new EmotionalCalibration();

        var roomTypeResolver = new MarkovRoomTypeResolver(
            new StaticRoomTypeMarkovMatrixProvider(),
            new MarkovTransitionResolver(new DeterministicMarkovSampler()),
            traceSink,
            calibration);

        var stateResolver = new MarkovPalaceRoomStateResolver(
            new MarkovTransitionResolver(new DeterministicMarkovSampler()),
            traceSink,
            calibration);

        return new DeterministicRunGenerator(
            new SeededRandomFactory(),
            roomTypeResolver,
            stateResolver,
            mapRoomGenerator,
            new RunPsycheEvolver(calibration));
    }
}
