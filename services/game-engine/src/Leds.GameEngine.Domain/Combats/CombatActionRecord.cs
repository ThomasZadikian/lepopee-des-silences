using Leds.GameEngine.Domain.Common;
using System.Text.Json;

namespace Leds.GameEngine.Domain.Combats;

public sealed class CombatActionRecord
{
    public Guid Id { get; }
    public Guid CombatId { get; }
    public int TurnNumber { get; }
    public Guid ActorId { get; }
    public string ActorSide { get; }
    public string SkillKey { get; }
    public string? SkillDisplayName { get; }
    public IReadOnlyCollection<Guid> TargetIds { get; }
    public string? SourceType { get; }
    public string? SourceKey { get; }

    public int RawDamage { get; }
    public int MitigatedDamage { get; }
    public int VitalityDamage { get; }
    public int GuardDamage { get; }
    public int GuardAbsorbed { get; }
    public int GuardGained { get; }

    public int DamageDealt { get; }
    public int DamageTaken { get; }
    public int HealingDone { get; }
    public int HealingReceived { get; }

    public IReadOnlyCollection<string> EffectsApplied { get; }
    public DateTime OccurredAtUtc { get; }

    private CombatActionRecord(
        Guid id,
        Guid combatId,
        int turnNumber,
        Guid actorId,
        string actorSide,
        string skillKey,
        string? skillDisplayName,
        IReadOnlyCollection<Guid> targetIds,
        string? sourceType,
        string? sourceKey,
        int rawDamage,
        int mitigatedDamage,
        int vitalityDamage,
        int guardDamage,
        int guardAbsorbed,
        int guardGained,
        int damageDealt,
        int damageTaken,
        int healingDone,
        int healingReceived,
        IReadOnlyCollection<string> effectsApplied,
        DateTime occurredAtUtc)
    {
        Id = id;
        CombatId = combatId;
        TurnNumber = turnNumber;
        ActorId = actorId;
        ActorSide = actorSide;
        SkillKey = skillKey;
        SkillDisplayName = skillDisplayName;
        TargetIds = targetIds;
        SourceType = sourceType;
        SourceKey = sourceKey;
        RawDamage = rawDamage;
        MitigatedDamage = mitigatedDamage;
        VitalityDamage = vitalityDamage;
        GuardDamage = guardDamage;
        GuardAbsorbed = guardAbsorbed;
        GuardGained = guardGained;
        DamageDealt = damageDealt;
        DamageTaken = damageTaken;
        HealingDone = healingDone;
        HealingReceived = healingReceived;
        EffectsApplied = effectsApplied;
        OccurredAtUtc = occurredAtUtc;
    }

    public static CombatActionRecord Create(
        Guid combatId,
        int turnNumber,
        Guid actorId,
        string actorSide,
        string skillKey,
        string? skillDisplayName,
        IReadOnlyCollection<Guid> targetIds,
        int rawDamage,
        int mitigatedDamage,
        int vitalityDamage,
        int guardDamage,
        int guardAbsorbed,
        int guardGained,
        int healingDone,
        int healingReceived,
        DateTime occurredAtUtc,
        string? sourceType = null,
        string? sourceKey = null,
        IReadOnlyCollection<string>? effectsApplied = null)
    {
        if (combatId == Guid.Empty)
            throw new DomainException("Combat id is required.");
        if (turnNumber <= 0)
            throw new DomainException("Turn number must be greater than zero.");
        if (actorId == Guid.Empty)
            throw new DomainException("Actor id is required.");
        if (string.IsNullOrWhiteSpace(actorSide))
            throw new DomainException("Actor side is required.");
        if (string.IsNullOrWhiteSpace(skillKey))
            throw new DomainException("Skill key is required.");

        var damageDealt = guardAbsorbed + vitalityDamage;
        var damageTaken = vitalityDamage + guardAbsorbed;

        return new CombatActionRecord(
            id: Guid.NewGuid(),
            combatId: combatId,
            turnNumber: turnNumber,
            actorId: actorId,
            actorSide: actorSide,
            skillKey: skillKey,
            skillDisplayName: skillDisplayName,
            targetIds: targetIds?.ToArray() ?? [],
            sourceType: sourceType,
            sourceKey: sourceKey,
            rawDamage: Math.Max(0, rawDamage),
            mitigatedDamage: Math.Max(0, mitigatedDamage),
            vitalityDamage: Math.Max(0, vitalityDamage),
            guardDamage: Math.Max(0, guardDamage),
            guardAbsorbed: Math.Max(0, guardAbsorbed),
            guardGained: Math.Max(0, guardGained),
            damageDealt: Math.Max(0, damageDealt),
            damageTaken: Math.Max(0, damageTaken),
            healingDone: Math.Max(0, healingDone),
            healingReceived: Math.Max(0, healingReceived),
            effectsApplied: effectsApplied?.ToArray() ?? [],
            occurredAtUtc: occurredAtUtc);
    }

    public static CombatActionRecord Rehydrate(
        Guid id,
        Guid combatId,
        int turnNumber,
        Guid actorId,
        string actorSide,
        string skillKey,
        string? skillDisplayName,
        IReadOnlyCollection<Guid> targetIds,
        string? sourceType,
        string? sourceKey,
        int rawDamage,
        int mitigatedDamage,
        int vitalityDamage,
        int guardDamage,
        int guardAbsorbed,
        int guardGained,
        int damageDealt,
        int damageTaken,
        int healingDone,
        int healingReceived,
        IReadOnlyCollection<string> effectsApplied,
        DateTime occurredAtUtc)
    {
        return new CombatActionRecord(
            id: id,
            combatId: combatId,
            turnNumber: turnNumber,
            actorId: actorId,
            actorSide: actorSide,
            skillKey: skillKey,
            skillDisplayName: skillDisplayName,
            targetIds: targetIds,
            sourceType: sourceType,
            sourceKey: sourceKey,
            rawDamage: rawDamage,
            mitigatedDamage: mitigatedDamage,
            vitalityDamage: vitalityDamage,
            guardDamage: guardDamage,
            guardAbsorbed: guardAbsorbed,
            guardGained: guardGained,
            damageDealt: damageDealt,
            damageTaken: damageTaken,
            healingDone: healingDone,
            healingReceived: healingReceived,
            effectsApplied: effectsApplied,
            occurredAtUtc: occurredAtUtc);
    }
}
