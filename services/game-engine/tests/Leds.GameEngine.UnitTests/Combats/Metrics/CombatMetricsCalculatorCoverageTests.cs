using FluentAssertions;
using Leds.GameEngine.Application.Combats.Metrics;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.UnitTests.Combats.Metrics;

public sealed class CombatMetricsCalculatorCoverageTests
{
    [Fact]
    public void CalculateActionRecords_ShouldCoverDamageHealingGuardAndMissingSnapshotPaths()
    {
        var actor = CombatantOf("actor", currentVitality: 90, guard: 15);
        var damaged = CombatantOf("damaged", currentVitality: 70, guard: 3);
        var healed = CombatantOf("healed", currentVitality: 90, guard: 12);
        var unchanged = CombatantOf("unchanged", currentVitality: 100, guard: 0);
        var skill = CombatantSkill.Create(
            "skill.metrics", "Metrics", "Damage", "SingleEnemy", "Damage",
            0, 0, 20, emotionalRegister: "Neutral");

        var before = new Dictionary<Guid, CombatantRuntimeStateSnapshot>
        {
            [actor.Id.Value] = new(80, 5),
            [damaged.Id.Value] = new(100, 10),
            [healed.Id.Value] = new(80, 5)
        };

        var records = CombatMetricsCalculator.CalculateActionRecords(
            Guid.NewGuid(), 2, actor, skill, [actor, damaged, healed, unchanged], before, DateTime.UnixEpoch)
            .ToArray();

        records.Should().HaveCount(4);

        var actorRecord = records.Single(record => record.TargetIds.Contains(actor.Id.Value));
        actorRecord.HealingReceived.Should().Be(10);
        actorRecord.HealingDone.Should().Be(10);
        actorRecord.GuardGained.Should().Be(10);

        var damageRecord = records.Single(record => record.TargetIds.Contains(damaged.Id.Value));
        damageRecord.VitalityDamage.Should().Be(30);
        damageRecord.GuardDamage.Should().Be(7);
        damageRecord.GuardAbsorbed.Should().Be(7);
        damageRecord.HealingReceived.Should().Be(0);
        damageRecord.MitigatedDamage.Should().Be(0);

        var healRecord = records.Single(record => record.TargetIds.Contains(healed.Id.Value));
        healRecord.HealingReceived.Should().Be(10);
        healRecord.HealingDone.Should().Be(10);
        healRecord.GuardGained.Should().Be(7);

        var unchangedRecord = records.Single(record => record.TargetIds.Contains(unchanged.Id.Value));
        unchangedRecord.VitalityDamage.Should().Be(0);
        unchangedRecord.HealingReceived.Should().Be(0);
        unchangedRecord.GuardDamage.Should().Be(0);
        unchangedRecord.GuardGained.Should().Be(0);
        unchangedRecord.MitigatedDamage.Should().Be(20);
    }

    [Fact]
    public void SnapshotTargets_ShouldCaptureEachTargetCurrentResources()
    {
        var first = CombatantOf("one", currentVitality: 77, guard: 4);
        var second = CombatantOf("two", currentVitality: 55, guard: 9);

        var snapshots = CombatMetricsCalculator.SnapshotTargets([first, second]);

        snapshots.Should().HaveCount(2);
        snapshots[first.Id.Value].Should().Be(new CombatantRuntimeStateSnapshot(77, 4));
        snapshots[second.Id.Value].Should().Be(new CombatantRuntimeStateSnapshot(55, 9));
    }

    [Fact]
    public void CalculateActionRecords_ShouldReturnEmptyForNoTargets()
    {
        var actor = CombatantOf("actor", 100, 0);
        var skill = CombatantSkill.Create(
            "skill.metrics", "Metrics", "Damage", "SingleEnemy", "Damage",
            0, 0, 10, emotionalRegister: "Neutral");

        CombatMetricsCalculator.CalculateActionRecords(
                Guid.NewGuid(), 1, actor, skill, [], new Dictionary<Guid, CombatantRuntimeStateSnapshot>(), DateTime.UtcNow)
            .Should().BeEmpty();
        CombatMetricsCalculator.SnapshotTargets([]).Should().BeEmpty();
    }

    private static Combatant CombatantOf(string key, int currentVitality, int guard) =>
        Combatant.Create(
            CombatantId.New(),
            key,
            key,
            CombatantSide.Player,
            "Fighter",
            maxVitality: 100,
            currentVitality: currentVitality,
            guard: guard,
            baseGuard: 0,
            mana: 0,
            charge: 0,
            maxMana: 100);
}
