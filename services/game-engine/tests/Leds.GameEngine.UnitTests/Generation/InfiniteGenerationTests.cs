using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Psyche;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;
using Leds.GameEngine.Infrastructure.Markov;

namespace Leds.GameEngine.UnitTests.Generation;

public sealed class InfiniteGenerationTests
{
    private const string Version = StaticRoomTypeMarkovMatrixProvider.SupportedVersion;

    // NOTE: adapte les arguments du constructeur si ton cablage differe (trace sink, calibration).
    private static MarkovRoomTypeResolver CreateResolver() =>
        new(
            new StaticRoomTypeMarkovMatrixProvider(),
            new MarkovTransitionResolver(new DeterministicMarkovSampler()),
            new NullMarkovTransitionTraceSink(),
            new EmotionalCalibration());

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(100)]
    public void Universal_room_counter_does_not_create_a_HimLit_boss_room(int depth)
    {
        var type = CreateResolver()
            .ResolveNextRoomType("seed-x", depth, RoomType.Antechamber, Version, null);

        type.Should().NotBe(RoomType.Final);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(19)]
    [InlineData(101)]
    public void Non_multiple_of_ten_is_a_normal_room(int depth)
    {
        var type = CreateResolver()
            .ResolveNextRoomType("seed-x", depth, RoomType.Memory, Version, null);

        type.Should().NotBe(RoomType.Final);
        type.Should().NotBe(RoomType.Threshold);
    }

    [Fact]
    public void Generation_is_not_capped_at_depth_ten()
    {
        var act = () => CreateResolver()
            .ResolveNextRoomType("seed-x", 251, RoomType.Memory, Version, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Resolving_after_a_boss_room_does_not_throw()
    {
        // depth 11, en sortant d'une room Him'Lit (Final)
        var act = () => CreateResolver()
            .ResolveNextRoomType("seed-x", 11, RoomType.Final, Version, null);

        act.Should().NotThrow();
    }
}
