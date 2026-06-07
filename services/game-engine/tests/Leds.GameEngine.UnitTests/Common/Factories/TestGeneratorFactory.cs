using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Infrastructure.Generation;
using Leds.GameEngine.Infrastructure.Generation.Randomness;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

namespace Leds.GameEngine.UnitTests.Common.Factories;

public static class TestGeneratorFactory
{
    public static DeterministicRunGenerator CreateDeterministicRunGenerator()
    {
        var mapRoomGenerator = new MapRoomGenerator(
            new RoomMapLayoutTemplateProvider(),
            new RoomThemeResolver(),
            new RoomBossProfileResolver());

        var roomTypeResolver = new MarkovRoomTypeResolver(
            new StaticRoomTypeMarkovMatrixProvider(),
            new MarkovTransitionResolver(new DeterministicMarkovSampler()));

        return new DeterministicRunGenerator(
            new SeededRandomFactory(),
            roomTypeResolver,
            mapRoomGenerator);
    }
}