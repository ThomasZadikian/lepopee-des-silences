using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Atb;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatHoldTickTests
{
    // Uniform speed across both sides (and zero Attack/Defense) keeps the live
    // tempo formula neutral (investment = 1.0, relative = 1.0), so the derived
    // AtbFillPerTick equals `speed` exactly — letting these tests set an exact
    // fill rate via stats instead of poking AtbFillPerTick directly (which is
    // now recomputed live on every HoldTick call, see AtbTempoFormula).
    private static Combat CreateSut(int allyCount = 2, int enemyCount = 1, int speed = 10)
    {
        var allies = Enumerable.Range(0, allyCount).Select(i =>
            Combatant.Create(
                CombatantId.New(), $"player.{i}", $"Hero{i}", CombatantSide.Player, "Fighter",
                maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
                speed: speed)).ToArray();

        var enemies = Enumerable.Range(0, enemyCount).Select(i =>
            Combatant.CreateEnemy($"enemy.{i}", $"Enemy{i}", "Guard", 80, speed: speed)).ToArray();

        return Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            allies,
            enemies);
    }

    [Fact]
    public void HoldTick_ShouldFreezeActiveAllyGauge_AtReadyThreshold_WhenItHoldsSelection()
    {
        var combat = CreateSut();
        var active = combat.Allies.First();
        active.SetAtbGauge(AtbConstants.ReadyThreshold);
        combat.MakeActiveCombatant(active.Id.Value);

        combat.HoldTick(100);

        active.AtbGauge.Should().Be(AtbConstants.ReadyThreshold);
    }

    [Fact]
    public void HoldTick_ShouldFreezeOtherAllies_WhileOneHoldsSelection()
    {
        var combat = CreateSut();
        var active = combat.Allies.First();
        var other = combat.Allies.Skip(1).First();
        active.SetAtbGauge(AtbConstants.ReadyThreshold);
        combat.MakeActiveCombatant(active.Id.Value);
        other.SetAtbGauge(1000);

        combat.HoldTick(100);

        other.AtbGauge.Should().Be(1000);
    }

    [Fact]
    public void HoldTick_ShouldCapGaugeAtReadyThreshold_OnLargeFill()
    {
        var combat = CreateSut(speed: 1_000_000);
        var enemy = combat.Enemies.First();
        enemy.SetAtbGauge(0);

        combat.HoldTick(100);

        enemy.AtbGauge.Should().Be(AtbConstants.ReadyThreshold);
    }

    [Fact]
    public void HoldTick_ShouldKeepFillingEnemies_WhileAllyHoldsSelection()
    {
        var combat = CreateSut(speed: 500);
        var active = combat.Allies.First();
        active.SetAtbGauge(AtbConstants.ReadyThreshold);
        combat.MakeActiveCombatant(active.Id.Value);
        var enemy = combat.Enemies.First();
        enemy.SetAtbGauge(0);

        combat.HoldTick(10);

        enemy.AtbGauge.Should().Be(5000);
    }

    [Fact]
    public void HoldTick_ShouldNotFreezeAnyAlly_WhenActiveAllyIsNotYetReady()
    {
        var combat = CreateSut(speed: 500);
        var active = combat.Allies.First();
        var other = combat.Allies.Skip(1).First();
        active.SetAtbGauge(0);
        combat.MakeActiveCombatant(active.Id.Value);
        other.SetAtbGauge(0);

        combat.HoldTick(10);

        other.AtbGauge.Should().Be(5000);
    }
}
