using FluentAssertions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.TacticalCombat;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.UnitTests.Runs.TacticalCombat;

public sealed class TacticalChargeRulesCoverageTests
{
    [Fact]
    public void AwardUsefulAction_ShouldIgnoreActionsWithoutVitalityImpact()
    {
        var actor = Combatant.CreateAlly("player.self", "Porteur", "Hero", 100);

        TacticalChargeRules.AwardUsefulAction(
            actor,
            [actor],
            [Impact(actor, vitalityDelta: 0)]);

        actor.Charge.Should().Be(0m);
    }

    [Fact]
    public void AwardUsefulAction_ShouldRewardActorAndLivingHealedAllyButNotTheActorTwice()
    {
        var actor = Combatant.CreateAlly("player.self", "Porteur", "Hero", 100);
        var ally = Combatant.CreateAlly("ally.one", "Allié", "Support", 100);

        TacticalChargeRules.AwardUsefulAction(
            actor,
            [actor, ally],
            [
                Impact(actor, vitalityDelta: 5),
                Impact(ally, vitalityDelta: 10),
            ]);

        actor.Charge.Should().Be(0.4m);
        ally.Charge.Should().Be(0.3m);
    }

    [Fact]
    public void AwardUsefulAction_ShouldIgnoreMissingHealingTargetAndCountDefeatsInActorGain()
    {
        var actor = Combatant.CreateAlly("player.self", "Porteur", "Hero", 100);
        var missingId = Guid.NewGuid();

        TacticalChargeRules.AwardUsefulAction(
            actor,
            [actor],
            [
                new TacticalImpactDto(missingId, 0, 0, 8, false),
                new TacticalImpactDto(Guid.NewGuid(), 0, 0, -25, true),
            ]);

        actor.Charge.Should().Be(0.7m);
    }

    [Fact]
    public void AwardUsefulAction_ShouldCapActorChargeGainAtTwo()
    {
        var actor = Combatant.CreateAlly("player.self", "Porteur", "Hero", 100);
        var impacts = Enumerable.Range(0, 12)
            .Select(_ => new TacticalImpactDto(Guid.NewGuid(), 0, 0, -1, true))
            .ToArray();

        TacticalChargeRules.AwardUsefulAction(actor, [actor], impacts);

        actor.Charge.Should().Be(2m);
    }

    private static TacticalImpactDto Impact(Combatant combatant, int vitalityDelta, bool defeated = false) =>
        new(combatant.Id.Value, 0, 0, vitalityDelta, defeated);
}
