using FluentAssertions;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// Authored NPC-pair social dynamics that indirectly color reputation gains:
/// Homoncule → Forgeron (amplifier), Homoncule &lt;-&gt; Enfant (mutual dislike),
/// Iris "juge à travers les yeux d'Ethan", and the Araran/Tovma/Mané mirrored trio.
/// See Run.ScaleReputationGain / Run.NarrativePairModifierPercent.
/// </summary>
public sealed class RunNarrativePairAffinityTests
{
    [Fact]
    public void ScaleReputationGain_ShouldBoostForgeron_WhenHomonculeLikesThePlayer()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AdjustNpcRelationshipScore("npc.homoncule", 250);

        run.ScaleReputationGain(10, "npc.forgeron").Should().Be(12); // +20%
    }

    [Fact]
    public void ScaleReputationGain_ShouldNotBoostForgeron_WhenHomonculeIsIndifferent()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.ScaleReputationGain(10, "npc.forgeron").Should().Be(10);
    }

    [Fact]
    public void ScaleReputationGain_ShouldPenalizeEnfant_WhenHomonculeLikesThePlayer()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AdjustNpcRelationshipScore("npc.homoncule", 250);

        run.ScaleReputationGain(10, "npc.enfant").Should().Be(7); // -30%
    }

    [Fact]
    public void ScaleReputationGain_ShouldPenalizeHomoncule_WhenEnfantLikesThePlayer()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AdjustNpcRelationshipScore("npc.enfant", 250);

        run.ScaleReputationGain(10, "npc.homoncule").Should().Be(7); // -30%, mutual
    }

    [Fact]
    public void ScaleReputationGain_ShouldPenalizeIris_WhenEthanDislikesThePlayer()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AdjustNpcRelationshipScore("npc.ethan", 10);
        run.GetNpcRelationship("npc.ethan")!.AdjustScore(-15); // net negative

        run.ScaleReputationGain(10, "npc.iris").Should().Be(7); // -30%
    }

    [Fact]
    public void ScaleReputationGain_ShouldNotPenalizeIris_WhenEthanIsNeutral()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.ScaleReputationGain(10, "npc.iris").Should().Be(10);
    }

    [Fact]
    public void ScaleReputationGain_ShouldPenalizeArarn_WhenTovmaDislikesThePlayer()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AdjustNpcRelationshipScore("npc.tovma", -5);

        run.ScaleReputationGain(10, "npc.araran").Should().Be(7); // -30%
    }

    [Fact]
    public void ScaleReputationGain_ShouldPenalizeTovma_WhenManeDislikesThePlayer()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AdjustNpcRelationshipScore("npc.mane", -5);

        run.ScaleReputationGain(10, "npc.tovma").Should().Be(7); // -30%
    }

    [Fact]
    public void ScaleReputationGain_ShouldPenalizeMane_WhenArarnDislikesThePlayer()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AdjustNpcRelationshipScore("npc.araran", -5);

        run.ScaleReputationGain(10, "npc.mane").Should().Be(7); // -30%
    }

    [Fact]
    public void ScaleReputationGain_ShouldStackWithReputationGainBonus_WhenBothApply()
    {
        var run = TestGameEngineFactory.CreateRun(reputationGainBonusPercent: 10);
        run.AdjustNpcRelationshipScore("npc.homoncule", 250);

        // Peluche de Mina (+10%) never applies to npc.homoncule itself (the bonus was
        // spent adjusting Homoncule's OWN score above) — this call targets Forgeron,
        // whose gain should reflect +10% (item) + 20% (Homoncule affinity) = +30%.
        run.ScaleReputationGain(10, "npc.forgeron").Should().Be(13);
    }

    [Fact]
    public void ScaleReputationGain_ShouldNeverApplyPairModifier_ToANegativeDelta()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AdjustNpcRelationshipScore("npc.homoncule", 250);

        run.ScaleReputationGain(-10, "npc.enfant").Should().Be(-10);
    }
}
