using FluentAssertions;
using Leds.GameEngine.Application.Events.Resolvers;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.UnitTests.Events.Resolvers;

public sealed class HimLitDialogueAttitudeResolverTests
{
    [Theory]
    [InlineData(1, "rencontre-p1-calme")]
    [InlineData(2, "rencontre-p2-calme")]
    [InlineData(4, "rencontre-p2-calme")]
    [InlineData(5, "rencontre-p3-calme")]
    [InlineData(9, "rencontre-p3-calme")]
    public void ResolveNodeKey_ShouldPickTierByTimesMet_WhenPrecedingRoomIsCalm(int timesMet, string expectedKey)
    {
        var key = HimLitDialogueAttitudeResolver.ResolveNodeKey(timesMet, PalaceRoomState.Neutral);

        key.Should().Be(expectedKey);
    }

    [Theory]
    [InlineData(PalaceRoomState.Neutral, false)]
    [InlineData(PalaceRoomState.Silent, false)]
    [InlineData(PalaceRoomState.Painful, true)]
    [InlineData(PalaceRoomState.Enraged, true)]
    [InlineData(PalaceRoomState.Violent, true)]
    public void ResolveNodeKey_ShouldSwitchToFracture_BasedOnPrecedingRoomState(
        PalaceRoomState precedingState, bool expectFracture)
    {
        var key = HimLitDialogueAttitudeResolver.ResolveNodeKey(1, precedingState);

        key.Should().Be(expectFracture ? "rencontre-p1-fracture" : "rencontre-p1-calme");
    }
}
