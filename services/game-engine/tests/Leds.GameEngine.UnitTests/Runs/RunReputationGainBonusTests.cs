using FluentAssertions;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>Mina's rare offering "Peluche de Mina" — +10% reputation gains, owned only.</summary>
public sealed class RunReputationGainBonusTests
{
    [Fact]
    public void ScaleReputationGain_ShouldScaleUpAPositiveDelta_WhenBonusIsSet()
    {
        var run = TestGameEngineFactory.CreateRun(reputationGainBonusPercent: 10);

        run.ScaleReputationGain(10).Should().Be(11);
    }

    [Fact]
    public void ScaleReputationGain_ShouldNotChangeANegativeDelta_WhenBonusIsSet()
    {
        var run = TestGameEngineFactory.CreateRun(reputationGainBonusPercent: 10);

        run.ScaleReputationGain(-5).Should().Be(-5,
            because: "the bonus must never soften a penalty (transgression).");
    }

    [Fact]
    public void ScaleReputationGain_ShouldNotChangeTheDelta_WhenBonusIsZero()
    {
        var run = TestGameEngineFactory.CreateRun(reputationGainBonusPercent: 0);

        run.ScaleReputationGain(10).Should().Be(10);
    }

    [Fact]
    public void AdjustNpcRelationshipScore_ShouldApplyTheBonus_ToANewRelationship()
    {
        var run = TestGameEngineFactory.CreateRun(reputationGainBonusPercent: 10);

        run.AdjustNpcRelationshipScore("npc.mane", 10);

        run.GetNpcRelationship("npc.mane")!.RelationshipScore.Should().Be(11);
    }

    // ---------------------------------------------------------------------------
    // "Loi du Nom Retenu" (ReputationChangeDoubled) — doubles BOTH gains and losses,
    // unlike ReputationGainBonusPercent above which only ever scales gains.
    // ---------------------------------------------------------------------------

    [Fact]
    public void ScaleReputationGain_ShouldDoubleAPositiveDelta_WhenReputationChangeDoubledIsActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.ReputationChangeDoubled, 1, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-nom-retenu-test"));

        run.ScaleReputationGain(10).Should().Be(20);
    }

    [Fact]
    public void ScaleReputationGain_ShouldDoubleANegativeDelta_WhenReputationChangeDoubledIsActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.ReputationChangeDoubled, 1, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-nom-retenu-test"));

        run.ScaleReputationGain(-5).Should().Be(-10,
            because: "Loi du Nom Retenu doubles losses too, unlike the ordinary reputation-gain bonus.");
    }

    [Fact]
    public void ScaleReputationGain_ShouldNotDoubleAnything_WhenReputationChangeDoubledIsNotActive()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.ScaleReputationGain(10).Should().Be(10);
        run.ScaleReputationGain(-5).Should().Be(-5);
    }

    [Fact]
    public void HimLitProtectionEnabled_ShouldDefaultToFalse()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.HimLitProtectionEnabled.Should().BeFalse();
    }

    [Fact]
    public void HimLitProtectionEnabled_ShouldBeTrue_WhenSetAtStartNew()
    {
        var run = TestGameEngineFactory.CreateRun(himLitProtectionEnabled: true);

        run.HimLitProtectionEnabled.Should().BeTrue();
    }
}
