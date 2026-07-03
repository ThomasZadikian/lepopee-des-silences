using FluentAssertions;
using Leds.GameEngine.Application.Combats.EnemyTurns.Ai;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Atb;

namespace Leds.GameEngine.UnitTests.Combats.EnemyTurns.Ai;

public sealed class EnemyBoostHeuristicTests
{
    private static Combatant CreateEnemy(string archetype)
        => Combatant.CreateEnemy($"enemy.{archetype}", "Enemy", archetype, 80);

    [Fact]
    public void ShouldHoldForMoreCharge_ShouldReturnFalse_ForIneligibleArchetype()
    {
        var enemy = CreateEnemy("swarm");
        enemy.SetAtbGauge(AtbConstants.ReadyThreshold);

        var result = EnemyBoostHeuristic.ShouldHoldForMoreCharge(enemy, Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldHoldForMoreCharge_ShouldBeDeterministic_AcrossRepeatedCalls()
    {
        var enemy = CreateEnemy("rupture");
        enemy.SetAtbGauge(AtbConstants.ReadyThreshold);
        var combatId = Guid.NewGuid();

        var first = EnemyBoostHeuristic.ShouldHoldForMoreCharge(enemy, combatId);
        var second = EnemyBoostHeuristic.ShouldHoldForMoreCharge(enemy, combatId);

        second.Should().Be(first);
    }

    [Fact]
    public void ShouldHoldForMoreCharge_ShouldReturnFalse_OnceGaugeReachesTheOverflowCap()
    {
        var enemy = CreateEnemy("boss");
        enemy.SetAtbGauge(AtbConstants.ReadyThreshold + AtbConstants.MaxChargeOverflow);

        var result = EnemyBoostHeuristic.ShouldHoldForMoreCharge(enemy, Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldHoldForMoreCharge_ShouldProduceAMixOfDecisions_AcrossManyEnemies()
    {
        var combatId = Guid.NewGuid();
        var decisions = Enumerable.Range(0, 200)
            .Select(_ =>
            {
                var enemy = CreateEnemy("elite");
                enemy.SetAtbGauge(AtbConstants.ReadyThreshold);
                return EnemyBoostHeuristic.ShouldHoldForMoreCharge(enemy, combatId);
            })
            .ToArray();

        decisions.Should().Contain(true);
        decisions.Should().Contain(false);
    }
}
