using FluentAssertions;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunAttackTypeItemTests
{
    // item:key:name:description:type:rarity:effectType:effectAmount  (4 == EmotionalType.Rupture)
    private const string RuptureMaskPayload =
        "item:item.relic.rupture-mask:Masque de Rupture:Retype les attaques du heros en Rupture.:Consumable:Uncommon:AttackTypeOverride:4";

    [Fact]
    public void ApplyAttackTypeItem_ShouldAddAttackTypeOverrideModifier()
    {
        var run = TestGameEngineFactory.CreateRun();
        ApplyRuptureMask(run);

        run.RunModifiers.Should().ContainSingle(
            m => m.Type == RunModifierType.AttackTypeOverride && !m.IsConsumed);
    }

    [Fact]
    public void ApplyAttackTypeItem_ShouldEncodeEmotionalTypeInValue()
    {
        var run = TestGameEngineFactory.CreateRun();
        ApplyRuptureMask(run);

        var modifier = run.RunModifiers
            .Single(m => m.Type == RunModifierType.AttackTypeOverride && !m.IsConsumed);

        modifier.Value.Should().Be((int)EmotionalType.Rupture);
        modifier.Duration.Should().Be(RunModifierDuration.UntilRunEnds);
    }

    [Fact]
    public void AttackTypeOverride_ShouldSurviveCombatEnd()
    {
        var run = TestGameEngineFactory.CreateRun();
        ApplyRuptureMask(run);

        run.ConsumeNextCombatModifiers();

        run.RunModifiers
            .Single(m => m.Type == RunModifierType.AttackTypeOverride)
            .IsConsumed.Should().BeFalse();
    }

    private static void ApplyRuptureMask(Run run)
    {
        var offerId = RewardOfferId.New();
        run.SetPendingRewardOffer(offerId);

        var choice = RewardChoice.Create(
            RewardType.TemporaryItem,
            "Masque de Rupture",
            "Retype les attaques du heros en Rupture.",
            RuptureMaskPayload);

        run.ApplyReward(choice);
        run.ClearPendingRewardOffer();
    }
}