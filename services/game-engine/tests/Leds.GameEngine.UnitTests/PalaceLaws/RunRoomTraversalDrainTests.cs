using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// "Dévoration" — <see cref="Run.OnRoomEnteredWithoutCombat"/> drains 3% of max vitality
/// when <see cref="RunModifierType.RoomTraversalHpDrain"/> is active and the player has
/// just traversed a room without resolving any combat node in it.
/// </summary>
public sealed class RunRoomTraversalDrainTests
{
    private static RunModifier CreateDrainModifier() => RunModifier.Create(
        RunModifierType.RoomTraversalHpDrain,
        value: 1,
        RunModifierDuration.UntilRunEnds,
        sourceType: "PalaceLaw",
        sourceKey: "law-devoration");

    [Fact]
    public void OnRoomEnteredWithoutCombat_ShouldDrainVitality_WhenTheModifierIsActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AddRunModifier(CreateDrainModifier());
        var hpBefore = run.CurrentHp;

        run.OnRoomEnteredWithoutCombat();

        run.CurrentHp.Should().Be(hpBefore - (int)Math.Round(run.MaxHp * 0.03));
    }

    [Fact]
    public void OnRoomEnteredWithoutCombat_ShouldDoNothing_WhenTheModifierIsNotActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        var hpBefore = run.CurrentHp;

        run.OnRoomEnteredWithoutCombat();

        run.CurrentHp.Should().Be(hpBefore);
    }

    [Fact]
    public void OnRoomEnteredWithoutCombat_ShouldNeverDropCurrentHpBelowOne()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AddRunModifier(CreateDrainModifier());
        run.ApplyVitalityLoss(run.CurrentHp - 1);
        run.CurrentHp.Should().Be(1);

        run.OnRoomEnteredWithoutCombat();

        run.CurrentHp.Should().Be(1, because: "an out-of-combat drain must never kill the player directly.");
    }

    [Fact]
    public void OnRoomEnteredWithoutCombat_ShouldIgnoreConsumedModifiers()
    {
        var run = TestGameEngineFactory.CreateRun();
        var modifier = CreateDrainModifier();
        run.AddRunModifier(modifier);
        modifier.Consume(DateTime.UtcNow);
        var hpBefore = run.CurrentHp;

        run.OnRoomEnteredWithoutCombat();

        run.CurrentHp.Should().Be(hpBefore);
    }

    // ---------------------------------------------------------------------------
    // "Dévoration" — the other half: <see cref="Run.CompleteActiveCombat()"/>
    // restores 5% of max vitality per combat WON, under the same modifier.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CompleteActiveCombat_ShouldRestoreFivePercentMaxVitality_WhenTheModifierIsActive()
    {
        var (run, combat, enemy) = CreateRunWithSelectedNodeAndCombat();
        run.AddRunModifier(CreateDrainModifier());
        run.ApplyVitalityLoss(run.MaxHp / 2);
        var hpBefore = run.CurrentHp;
        // OnCombatantDefeated is post-defeat bookkeeping (initiative removal) — it does
        // not itself reduce vitality. HasLivingEnemies (and so CompleteIfAllEnemiesDefeated)
        // reads Combatant.IsDefeated, which only flips once damage brings vitality to zero.
        enemy.ApplyVitalityDamage(enemy.CurrentVitality);
        combat.OnCombatantDefeated(enemy.Id.Value);
        combat.CompleteIfAllEnemiesDefeated();

        run.CompleteActiveCombat();

        run.CurrentHp.Should().Be(hpBefore + (int)Math.Round(run.MaxHp * 0.05));
    }

    [Fact]
    public void CompleteActiveCombat_ShouldNotRestoreVitality_WhenTheModifierIsNotActive()
    {
        var (run, combat, enemy) = CreateRunWithSelectedNodeAndCombat();
        run.ApplyVitalityLoss(run.MaxHp / 2);
        var hpBefore = run.CurrentHp;
        // OnCombatantDefeated is post-defeat bookkeeping (initiative removal) — it does
        // not itself reduce vitality. HasLivingEnemies (and so CompleteIfAllEnemiesDefeated)
        // reads Combatant.IsDefeated, which only flips once damage brings vitality to zero.
        enemy.ApplyVitalityDamage(enemy.CurrentVitality);
        combat.OnCombatantDefeated(enemy.Id.Value);
        combat.CompleteIfAllEnemiesDefeated();

        run.CompleteActiveCombat();

        run.CurrentHp.Should().Be(hpBefore);
    }

    [Fact]
    public void CompleteActiveCombat_ShouldNotRestoreVitality_WhenAlreadyAtFullHp()
    {
        var (run, combat, enemy) = CreateRunWithSelectedNodeAndCombat();
        run.AddRunModifier(CreateDrainModifier());
        // OnCombatantDefeated is post-defeat bookkeeping (initiative removal) — it does
        // not itself reduce vitality. HasLivingEnemies (and so CompleteIfAllEnemiesDefeated)
        // reads Combatant.IsDefeated, which only flips once damage brings vitality to zero.
        enemy.ApplyVitalityDamage(enemy.CurrentVitality);
        combat.OnCombatantDefeated(enemy.Id.Value);
        combat.CompleteIfAllEnemiesDefeated();

        run.CompleteActiveCombat();

        run.CurrentHp.Should().Be(run.MaxHp);
    }

    private static (Run Run, TacticalCombat Combat, Combatant Enemy) CreateRunWithSelectedNodeAndCombat()
    {
        var run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat).Run;
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var (_, combat) = TestTacticalCombatHelper.CreateRunWithCombat(run, [ally], [enemy]);

        return (run, combat, enemy);
    }
}
