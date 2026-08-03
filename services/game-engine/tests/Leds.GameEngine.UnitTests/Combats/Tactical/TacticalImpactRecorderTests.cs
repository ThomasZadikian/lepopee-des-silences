using FluentAssertions;
using Leds.GameEngine.Application.Runs.TacticalCombat;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

/// <summary>
/// A hit fully absorbed by Guard left CurrentVitality untouched, so the plain before/after
/// vitality diff produced no impact at all — no floating number, no sign anything happened.
/// Regression coverage for surfacing Guard's own ledger alongside vitality.
/// </summary>
public sealed class TacticalImpactRecorderTests
{
    private static Domain.Combats.Tactical.TacticalCombat CreateCombat(
        Combatant ally, Combatant enemy) =>
        TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);

    [Fact]
    public void Diff_ShouldReportGuardAbsorbed_WhenAHitIsFullyAbsorbed()
    {
        var ally = Combatant.CreateAlly("player.1", "Ariane", "Guard", 40, baseGuard: 20);
        var enemy = Combatant.CreateEnemy("enemy.1", "Brute", "Bruiser", 30);
        var combat = CreateCombat(ally, enemy);

        var before = TacticalImpactRecorder.Capture([ally]);
        ally.ApplyDamage(8); // fully absorbed: 8 <= 20 of guard, vitality untouched

        var impacts = TacticalImpactRecorder.Diff(before, [ally], combat);

        var impact = impacts.Should().ContainSingle().Which;
        impact.VitalityDelta.Should().Be(0);
        impact.GuardAbsorbed.Should().Be(8);
        impact.Missed.Should().BeFalse();
    }

    [Fact]
    public void Diff_ShouldReportBothVitalityLossAndGuardAbsorbed_WhenAHitSpillsOverGuard()
    {
        var ally = Combatant.CreateAlly("player.1", "Ariane", "Guard", 40, baseGuard: 5);
        var enemy = Combatant.CreateEnemy("enemy.1", "Brute", "Bruiser", 30);
        var combat = CreateCombat(ally, enemy);

        var before = TacticalImpactRecorder.Capture([ally]);
        ally.ApplyDamage(12); // 5 absorbed by guard, 7 spills onto vitality

        var impacts = TacticalImpactRecorder.Diff(before, [ally], combat);

        var impact = impacts.Should().ContainSingle().Which;
        impact.VitalityDelta.Should().Be(7);
        impact.GuardAbsorbed.Should().Be(5);
    }

    [Fact]
    public void Diff_ShouldReportNoGuardAbsorbed_WhenTheTargetHasNoGuard()
    {
        var ally = Combatant.CreateAlly("player.1", "Ariane", "Guard", 40);
        var enemy = Combatant.CreateEnemy("enemy.1", "Brute", "Bruiser", 30);
        var combat = CreateCombat(ally, enemy);

        var before = TacticalImpactRecorder.Capture([ally]);
        ally.ApplyDamage(10);

        var impacts = TacticalImpactRecorder.Diff(before, [ally], combat);

        var impact = impacts.Should().ContainSingle().Which;
        impact.VitalityDelta.Should().Be(10);
        impact.GuardAbsorbed.Should().Be(0);
    }

    [Fact]
    public void Diff_ShouldProduceNoImpact_WhenNothingChangedAtAll()
    {
        var ally = Combatant.CreateAlly("player.1", "Ariane", "Guard", 40);
        var enemy = Combatant.CreateEnemy("enemy.1", "Brute", "Bruiser", 30);
        var combat = CreateCombat(ally, enemy);

        var before = TacticalImpactRecorder.Capture([ally]);

        var impacts = TacticalImpactRecorder.Diff(before, [ally], combat);

        impacts.Should().BeEmpty();
    }
}
