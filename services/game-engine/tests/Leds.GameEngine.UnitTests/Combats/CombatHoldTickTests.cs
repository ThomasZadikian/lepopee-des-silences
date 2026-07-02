using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Atb;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatHoldTickTests
{
    private static Combat CreateSut(int allyCount = 2, int enemyCount = 1)
    {
        var allies = Enumerable.Range(0, allyCount).Select(i =>
            Combatant.CreateAlly($"player.{i}", $"Hero{i}", "Fighter", 100)).ToArray();

        var enemies = Enumerable.Range(0, enemyCount).Select(i =>
            Combatant.CreateEnemy($"enemy.{i}", $"Enemy{i}", "Guard", 80)).ToArray();

        return Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            allies,
            enemies);
    }

    [Fact]
    public void HoldTick_ShouldKeepFillingActiveAllyGauge_PastReadyThreshold_WhenItHoldsSelection()
    {
        var combat = CreateSut();
        var active = combat.Allies.First();
        active.SetAtbGauge(AtbConstants.ReadyThreshold);
        active.SetAtbFillPerTick(1000);
        combat.MakeActiveCombatant(active.Id.Value);

        combat.HoldTick(100);

        active.AtbGauge.Should().BeGreaterThan(AtbConstants.ReadyThreshold);
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
        other.SetAtbFillPerTick(500);

        combat.HoldTick(100);

        other.AtbGauge.Should().Be(1000);
    }

    [Fact]
    public void HoldTick_ShouldCapActiveAllyGauge_AtReadyThresholdPlusMaxOverflow()
    {
        var combat = CreateSut();
        var active = combat.Allies.First();
        active.SetAtbGauge(AtbConstants.ReadyThreshold);
        active.SetAtbFillPerTick(1_000_000);
        combat.MakeActiveCombatant(active.Id.Value);

        combat.HoldTick(100);

        active.AtbGauge.Should().Be(AtbConstants.ReadyThreshold + AtbConstants.MaxChargeOverflow);
    }

    [Fact]
    public void HoldTick_ShouldKeepFillingEnemies_WhileAllyHoldsSelection()
    {
        var combat = CreateSut();
        var active = combat.Allies.First();
        active.SetAtbGauge(AtbConstants.ReadyThreshold);
        combat.MakeActiveCombatant(active.Id.Value);
        var enemy = combat.Enemies.First();
        enemy.SetAtbGauge(0);
        enemy.SetAtbFillPerTick(500);

        combat.HoldTick(10);

        enemy.AtbGauge.Should().Be(5000);
    }

    [Fact]
    public void HoldTick_ShouldNotFreezeAnyAlly_WhenActiveAllyIsNotYetReady()
    {
        var combat = CreateSut();
        var active = combat.Allies.First();
        var other = combat.Allies.Skip(1).First();
        active.SetAtbGauge(0);
        combat.MakeActiveCombatant(active.Id.Value);
        other.SetAtbGauge(0);
        other.SetAtbFillPerTick(500);

        combat.HoldTick(10);

        other.AtbGauge.Should().Be(5000);
    }
}
