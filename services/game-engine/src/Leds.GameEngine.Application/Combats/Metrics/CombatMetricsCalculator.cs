using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Metrics;

public static class CombatMetricsCalculator
{
    public static IReadOnlyCollection<CombatActionRecord> CalculateActionRecords(
        Guid combatId,
        int turnNumber,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        IReadOnlyDictionary<Guid, CombatantRuntimeStateSnapshot> beforeSnapshots,
        DateTime occurredAtUtc)
    {
        var records = new List<CombatActionRecord>(targets.Count);

        foreach (var target in targets)
        {
            var before = beforeSnapshots.TryGetValue(target.Id.Value, out var snapshot)
                ? snapshot
                : new CombatantRuntimeStateSnapshot(target.CurrentVitality, target.Guard);

            var vitalityDelta = target.CurrentVitality - before.CurrentVitality;
            var guardDelta = target.Guard - before.CurrentGuard;

            int vitalityDamage = 0;
            int guardAbsorbed = 0;
            int guardDamage = 0;
            int guardGained = 0;
            int healingReceived = 0;

            if (vitalityDelta < 0)
            {
                vitalityDamage = -vitalityDelta;
            }
            else if (vitalityDelta > 0)
            {
                healingReceived = vitalityDelta;
            }

            if (guardDelta < 0)
            {
                guardDamage = -guardDelta;
                guardAbsorbed = -guardDelta;
            }
            else if (guardDelta > 0)
            {
                guardGained = guardDelta;
            }

            var rawDamage = skill.BasePower;
            var mitigatedDamage = Math.Max(0, rawDamage - (guardAbsorbed + vitalityDamage));

            int healingDone = 0;
            if (actor.Id == target.Id)
            {
                healingDone = healingReceived;
            }
            else if (healingReceived > 0)
            {
                healingDone = healingReceived;
            }

            var record = CombatActionRecord.Create(
                combatId: combatId,
                turnNumber: turnNumber,
                actorId: actor.Id.Value,
                actorSide: actor.Side.ToString(),
                skillKey: skill.Key,
                skillDisplayName: skill.DisplayName,
                targetIds: [target.Id.Value],
                rawDamage: rawDamage,
                mitigatedDamage: mitigatedDamage,
                vitalityDamage: vitalityDamage,
                guardDamage: guardDamage,
                guardAbsorbed: guardAbsorbed,
                guardGained: guardGained,
                healingDone: healingDone,
                healingReceived: healingReceived,
                occurredAtUtc: occurredAtUtc,
                sourceType: "skill",
                sourceKey: skill.Key);

            records.Add(record);
        }

        return records;
    }

    public static Dictionary<Guid, CombatantRuntimeStateSnapshot> SnapshotTargets(
        IReadOnlyCollection<Combatant> targets)
    {
        var snapshots = new Dictionary<Guid, CombatantRuntimeStateSnapshot>(targets.Count);
        foreach (var target in targets)
        {
            snapshots[target.Id.Value] = new CombatantRuntimeStateSnapshot(
                target.CurrentVitality,
                target.Guard);
        }
        return snapshots;
    }
}

public sealed record CombatantRuntimeStateSnapshot(int CurrentVitality, int CurrentGuard);
