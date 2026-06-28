using FluentAssertions;
using Leds.GameEngine.Domain.Effects;

namespace Leds.GameEngine.UnitTests.Effects;

public sealed class EffectDurationTests
{
    [Fact]
    public void EffectDuration_ShouldContainImmediate()
    {
        Enum.GetValues<EffectDuration>().Should().Contain(EffectDuration.Immediate);
    }

    [Fact]
    public void EffectDuration_ShouldContainCurrentCombat()
    {
        Enum.GetValues<EffectDuration>().Should().Contain(EffectDuration.CurrentCombat);
    }

    [Fact]
    public void EffectDuration_ShouldContainNextCombatOnly()
    {
        Enum.GetValues<EffectDuration>().Should().Contain(EffectDuration.NextCombatOnly);
    }

    [Fact]
    public void EffectDuration_ShouldContainNextRewardOnly()
    {
        Enum.GetValues<EffectDuration>().Should().Contain(EffectDuration.NextRewardOnly);
    }

    [Fact]
    public void EffectDuration_ShouldContainUntilRoomEnds()
    {
        Enum.GetValues<EffectDuration>().Should().Contain(EffectDuration.UntilRoomEnds);
    }

    [Fact]
    public void EffectDuration_ShouldContainUntilRunEnds()
    {
        Enum.GetValues<EffectDuration>().Should().Contain(EffectDuration.UntilRunEnds);
    }

    [Fact]
    public void EffectDuration_ShouldContainPermanentCandidate()
    {
        Enum.GetValues<EffectDuration>().Should().Contain(EffectDuration.PermanentCandidate);
    }

    [Fact]
    public void EffectDuration_ShouldContainUntilConsumed()
    {
        Enum.GetValues<EffectDuration>().Should().Contain(EffectDuration.UntilConsumed);
    }

    [Fact]
    public void EffectDuration_ShouldHaveEightValues()
    {
        Enum.GetValues<EffectDuration>().Should().HaveCount(8);
    }
}
