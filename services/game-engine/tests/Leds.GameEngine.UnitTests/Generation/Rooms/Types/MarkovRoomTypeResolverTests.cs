using FluentAssertions;
using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

namespace Leds.GameEngine.UnitTests.Generation.Rooms.Types;

public sealed class MarkovRoomTypeResolverTests
{
    private readonly MarkovRoomTypeResolver _resolver = new(
        new StaticRoomTypeMarkovMatrixProvider(),
        new MarkovTransitionResolver(new DeterministicMarkovSampler()));

    [Fact]
    public void ResolveNextRoomType_ShouldReturnThreshold_WhenDepthIsZero()
    {
        var roomType = _resolver.ResolveNextRoomType(
            seed: "seed-test-001",
            nextRoomDepth: 0,
            currentRoomType: RoomType.Memory,
            matrixVersion: StaticRoomTypeMarkovMatrixProvider.SupportedVersion);

        roomType.Should().Be(RoomType.Threshold);
    }

    [Fact]
    public void ResolveNextRoomType_ShouldReturnFinal_WhenDepthIsTen()
    {
        var roomType = _resolver.ResolveNextRoomType(
            seed: "seed-test-001",
            nextRoomDepth: 10,
            currentRoomType: RoomType.Memory,
            matrixVersion: StaticRoomTypeMarkovMatrixProvider.SupportedVersion);

        roomType.Should().Be(RoomType.Final);
    }

    [Fact]
    public void ResolveNextRoomType_ShouldBeDeterministic_ForSameInputs()
    {
        var first = _resolver.ResolveNextRoomType(
            seed: "seed-test-001",
            nextRoomDepth: 3,
            currentRoomType: RoomType.Memory,
            matrixVersion: StaticRoomTypeMarkovMatrixProvider.SupportedVersion);

        var second = _resolver.ResolveNextRoomType(
            seed: "seed-test-001",
            nextRoomDepth: 3,
            currentRoomType: RoomType.Memory,
            matrixVersion: StaticRoomTypeMarkovMatrixProvider.SupportedVersion);

        second.Should().Be(first);
    }

    [Fact]
    public void ResolveNextRoomType_ShouldReturnPlayableRoomType_ForIntermediateDepth()
    {
        var roomType = _resolver.ResolveNextRoomType(
            seed: "seed-test-001",
            nextRoomDepth: 3,
            currentRoomType: RoomType.Memory,
            matrixVersion: StaticRoomTypeMarkovMatrixProvider.SupportedVersion);

        roomType.Should().NotBe(RoomType.Threshold);
        roomType.Should().NotBe(RoomType.Final);
        roomType.Should().BeOneOf(
            RoomType.Memory,
            RoomType.Forest,
            RoomType.Rupture,
            RoomType.Silence,
            RoomType.Antechamber);
    }

    [Fact]
    public void ResolveNextRoomType_ShouldThrow_WhenSeedIsBlank()
    {
        var act = () => _resolver.ResolveNextRoomType(
            seed: " ",
            nextRoomDepth: 3,
            currentRoomType: RoomType.Memory,
            matrixVersion: StaticRoomTypeMarkovMatrixProvider.SupportedVersion);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Seed is required.*");
    }

    [Fact]
    public void ResolveNextRoomType_ShouldThrow_WhenMatrixVersionIsUnsupported()
    {
        var act = () => _resolver.ResolveNextRoomType(
            seed: "seed-test-001",
            nextRoomDepth: 3,
            currentRoomType: RoomType.Memory,
            matrixVersion: "markov-room-type-unknown");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unsupported room type Markov matrix version 'markov-room-type-unknown'.");
    }

    [Fact]
    public void ResolveNextRoomType_ShouldThrow_WhenCurrentRoomIsFinal()
    {
        var act = () => _resolver.ResolveNextRoomType(
            seed: "seed-test-001",
            nextRoomDepth: 3,
            currentRoomType: RoomType.Final,
            matrixVersion: StaticRoomTypeMarkovMatrixProvider.SupportedVersion);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot resolve a next room type from the final room.");
    }
}