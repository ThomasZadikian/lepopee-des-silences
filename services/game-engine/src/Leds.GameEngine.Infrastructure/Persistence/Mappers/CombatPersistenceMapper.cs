using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Entities;
using System.Text.Json;

namespace Leds.GameEngine.Infrastructure.Persistence.Mappers;

public static class CombatPersistenceMapper
{
    // -----------------------------------------------------------------------
    // Domain → Entity
    // -----------------------------------------------------------------------

    public static CombatantEntity ToEntity(Combatant combatant, Guid combatId)
    {
        return new CombatantEntity
        {
            Id = combatant.Id.Value,
            CombatId = combatId,
            SourceKey = combatant.SourceKey,
            CharacterInstanceId = combatant.CharacterInstanceId,
            DisplayName = combatant.DisplayName,
            Side = combatant.Side.ToString(),
            Archetype = combatant.Archetype,
            NaturalEmotionalRegister = combatant.NaturalEmotionalType.ToString(),
            MaxVitality = combatant.MaxVitality,
            CurrentVitality = combatant.CurrentVitality,
            Guard = combatant.Guard,
            BaseGuard = combatant.BaseGuard,
            Mana = combatant.Mana,
            MaxMana = combatant.MaxMana,
            Charge = combatant.Charge,
            Status = combatant.Status.ToString(),
            HasActedThisCombat = combatant.HasActedThisCombat,
            AttackTypeOverride = combatant.AttackTypeOverride.HasValue ? (int)combatant.AttackTypeOverride.Value : null,
            TypedDamageReductionsJson = SerializeTypedDamageReductions(combatant.TypedDamageReductionPercent),
            HitChanceBonusPercent = combatant.HitChanceBonusPercent,
            DotDurationReductionPercent = combatant.DotDurationReductionPercent,
            DotDamageReductionPercent = combatant.DotDamageReductionPercent,
            DotDamageBonusPercent = combatant.DotDamageBonusPercent,
            MagicDamageBonusPercent = combatant.MagicDamageBonusPercent,
            MagicDamageReductionPercent = combatant.MagicDamageReductionPercent,
            CriticalChanceBonusPercent = combatant.CriticalChanceBonusPercent,
            HealingBonusPercent = combatant.HealingBonusPercent,
            StatusEffectsJson = SerializeStatusEffects(combatant.StatusEffects),
            // PermanentSkills, not Skills — Skills also includes anything temporarily
            // granted by a SkillGrant status effect (e.g. "Création"), which must NOT
            // be persisted as a permanent skill entity; it's carried by the status
            // effect itself (see StatusEffectSnapshot.GrantedSkills below) instead.
            Skills = combatant.PermanentSkills.Select(s => ToEntity(s, combatant.Id.Value)).ToList(),
            BaseStatSnapshot = ToBaseStatSnapshotEntity(combatant.BaseStatSnapshot, combatant.Id.Value),
            RuntimeState = ToRuntimeStateEntity(combatant.RuntimeState, combatant.Id.Value)
        };
    }

    private static string? SerializeTypedDamageReductions(IReadOnlyDictionary<EmotionalType, int> reductions)
    {
        if (reductions.Count == 0)
            return null;

        var snapshot = reductions.ToDictionary(kv => (int)kv.Key, kv => kv.Value);
        return JsonSerializer.Serialize(snapshot);
    }

    private static IReadOnlyDictionary<EmotionalType, int> DeserializeTypedDamageReductions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<EmotionalType, int>();

        var snapshot = JsonSerializer.Deserialize<Dictionary<int, int>>(json);
        if (snapshot is null)
            return new Dictionary<EmotionalType, int>();

        return snapshot.ToDictionary(kv => (EmotionalType)kv.Key, kv => kv.Value);
    }

    private sealed record StatusEffectSnapshot(
        string Key,
        string DisplayName,
        int Kind,
        int? EmotionalType,
        int Stat,
        int Magnitude,
        int Stacks,
        int TickInterval,
        int NextTickAtTick,
        int ExpiresAtTick,
        bool IsMagnitudePercentOfMax = false,
        bool IsMagnitudePercentOfBaseStat = false,
        GrantedSkillSnapshot[]? GrantedSkills = null,
        bool IsPermanent = false,
        Guid?[]? StackSourceIds = null);

    // Kind == SkillGrant only (e.g. "Création") — a snapshot of the target's skills
    // at cast time, temporarily usable by whoever holds the status effect. Mirrors
    // CombatantSkillEntity's field shape so the round-trip is lossless.
    private sealed record GrantedSkillSnapshot(
        string Key, string DisplayName, string SkillType, string TargetingType, string EffectType,
        int ManaCost, int ChargeCost, int BasePower, string[] Tags, string Category,
        bool BasePowerIsPercentOfMaxVitality);

    private static string? SerializeStatusEffects(IReadOnlyCollection<CombatStatusEffect> effects)
    {
        if (effects.Count == 0)
            return null;

        var snapshots = effects.Select(e => new StatusEffectSnapshot(
            e.Key,
            e.DisplayName,
            (int)e.Kind,
            e.EmotionalType.HasValue ? (int)e.EmotionalType.Value : null,
            (int)e.Stat,
            e.Magnitude,
            e.Stacks,
            e.TickInterval,
            e.NextTickAtTick,
            e.ExpiresAtTick,
            e.IsMagnitudePercentOfMax,
            e.IsMagnitudePercentOfBaseStat,
            e.GrantedSkills.Count == 0
                ? null
                : e.GrantedSkills.Select(s => new GrantedSkillSnapshot(
                    s.Key, s.DisplayName, s.SkillType, s.TargetingType, s.EffectType,
                    s.ManaCost, s.ChargeCost, s.BasePower, s.Tags.ToArray(), s.Category,
                    s.BasePowerIsPercentOfMaxVitality)).ToArray(),
            e.IsPermanent,
            e.StackSourceIds.ToArray())).ToArray();

        return JsonSerializer.Serialize(snapshots);
    }

    private static IEnumerable<CombatStatusEffect> DeserializeStatusEffects(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            yield break;

        var snapshots = JsonSerializer.Deserialize<StatusEffectSnapshot[]>(json);
        if (snapshots is null)
            yield break;

        foreach (var s in snapshots)
        {
            var grantedSkills = s.GrantedSkills?
                .Select(g => CombatantSkill.Rehydrate(
                    g.Key, g.DisplayName, g.SkillType, g.TargetingType, g.EffectType,
                    g.ManaCost, g.ChargeCost, g.BasePower, g.Tags,
                    category: g.Category,
                    basePowerIsPercentOfMaxVitality: g.BasePowerIsPercentOfMaxVitality))
                .ToArray();

            yield return CombatStatusEffect.Rehydrate(
                s.Key,
                s.DisplayName,
                (StatusEffectKind)s.Kind,
                s.EmotionalType.HasValue ? (EmotionalType)s.EmotionalType.Value : null,
                (CombatStat)s.Stat,
                s.Magnitude,
                s.Stacks,
                s.TickInterval,
                s.NextTickAtTick,
                s.ExpiresAtTick,
                s.IsMagnitudePercentOfMax,
                s.IsMagnitudePercentOfBaseStat,
                grantedSkills,
                s.IsPermanent,
                s.StackSourceIds);
        }
    }

    public static CombatantBaseStatSnapshotEntity ToBaseStatSnapshotEntity(CombatantBaseStatSnapshot snapshot, Guid combatantId)
    {
        return new CombatantBaseStatSnapshotEntity
        {
            Id = snapshot.Id,
            CombatantId = combatantId,
            MaxVitality = snapshot.MaxVitality,
            AttackPower = snapshot.AttackPower,
            Defense = snapshot.Defense,
            StartingGuard = snapshot.StartingGuard,
            Speed = snapshot.Speed,
            Initiative = snapshot.Initiative,
            Focus = snapshot.Focus,
            Mana = snapshot.Mana,
            Charge = snapshot.Charge,
            MagicAttack = snapshot.MagicAttack,
            MagicDefense = snapshot.MagicDefense,
            Movement = snapshot.Movement,
            CreatedAtUtc = snapshot.CreatedAtUtc
        };
    }

    public static CombatantRuntimeStateEntity ToRuntimeStateEntity(CombatantRuntimeState state, Guid combatantId)
    {
        return new CombatantRuntimeStateEntity
        {
            Id = state.Id,
            CombatantId = combatantId,
            CurrentVitality = state.CurrentVitality,
            CurrentGuard = state.CurrentGuard,
            CurrentFocus = state.CurrentFocus,
            CurrentMana = state.CurrentMana,
            MaxMana = state.MaxMana,
            CurrentCharge = state.CurrentCharge,
            ThreatValue = state.ThreatValue,
            LastAttackerId = state.LastAttackerId,
            TookPowerfulHitSinceLastAction = state.TookPowerfulHitSinceLastAction,
            UpdatedAtUtc = state.UpdatedAtUtc
        };
    }

    public static CombatantSkillEntity ToEntity(CombatantSkill skill, Guid combatantId)
    {
        return new CombatantSkillEntity
        {
            Id = Guid.NewGuid(),
            CombatantId = combatantId,
            Key = skill.Key,
            DisplayName = skill.DisplayName,
            SkillType = skill.SkillType,
            TargetingType = skill.TargetingType,
            EffectType = skill.EffectType,
            ManaCost = skill.ManaCost,
            ChargeCost = skill.ChargeCost,
            BasePower = skill.BasePower,
            Tags = JsonSerializer.Serialize(skill.Tags),
            Category = skill.Category,
            BasePowerIsPercentOfMaxVitality = skill.BasePowerIsPercentOfMaxVitality,
            TacticalRange = skill.TacticalRange,
            TacticalAreaShape = skill.TacticalAreaShape.ToString(),
            RequiresLineOfSight = skill.RequiresLineOfSight,
            Cooldown = skill.Cooldown,
            IsUltimate = skill.IsUltimate,
            EmotionalRegister = skill.EmotionalRegister,
            StatusEffectsJson = skill.StatusEffects is { Count: > 0 }
                ? JsonSerializer.Serialize(skill.StatusEffects)
                : null
        };
    }

    // -----------------------------------------------------------------------
    // Entity → Domain
    // -----------------------------------------------------------------------

    public static Combatant ToDomain(CombatantEntity entity)
    {
        var baseStatSnapshot = entity.BaseStatSnapshot is not null
            ? ToDomainBaseStatSnapshot(entity.BaseStatSnapshot)
            : null;

        var runtimeState = entity.RuntimeState is not null
            ? ToDomainRuntimeState(entity.RuntimeState)
            : null;

        var combatant = Combatant.Rehydrate(
            new CombatantId(entity.Id),
            entity.SourceKey,
            entity.DisplayName,
            Enum.Parse<CombatantSide>(entity.Side),
            entity.Archetype,
            entity.MaxVitality,
            entity.CurrentVitality,
            entity.Guard,
            entity.BaseGuard,
            entity.Mana,
            entity.Charge,
            Enum.Parse<CombatantStatus>(entity.Status),
            entity.Skills.Select(ToDomain).ToList(),
            baseStatSnapshot: baseStatSnapshot,
            runtimeState: runtimeState,
            maxMana: entity.MaxMana,
            attackTypeOverride: entity.AttackTypeOverride.HasValue ? (EmotionalType)entity.AttackTypeOverride.Value : null,
            typedDamageReductionPercent: DeserializeTypedDamageReductions(entity.TypedDamageReductionsJson),
            hitChanceBonusPercent: entity.HitChanceBonusPercent,
            dotDurationReductionPercent: entity.DotDurationReductionPercent,
            dotDamageReductionPercent: entity.DotDamageReductionPercent,
            magicDamageBonusPercent: entity.MagicDamageBonusPercent,
            magicDamageReductionPercent: entity.MagicDamageReductionPercent,
            criticalChanceBonusPercent: entity.CriticalChanceBonusPercent,
            dotDamageBonusPercent: entity.DotDamageBonusPercent,
            healingBonusPercent: entity.HealingBonusPercent,
            hasActedThisCombat: entity.HasActedThisCombat,
            naturalEmotionalType: EmotionalTypeCode.ParseRequired(
                entity.NaturalEmotionalRegister,
                $"Combatant '{entity.SourceKey}' natural emotional register"),
            characterInstanceId: entity.CharacterInstanceId);
        foreach (var effect in DeserializeStatusEffects(entity.StatusEffectsJson))
            combatant.RehydrateStatusEffect(effect);

        return combatant;
    }

    public static CombatantBaseStatSnapshot ToDomainBaseStatSnapshot(CombatantBaseStatSnapshotEntity entity)
    {
        return CombatantBaseStatSnapshot.Rehydrate(
            entity.Id,
            entity.MaxVitality,
            entity.AttackPower,
            entity.Defense,
            entity.StartingGuard,
            entity.Speed,
            entity.Initiative,
            entity.Focus,
            entity.Mana,
            entity.Charge,
            entity.CreatedAtUtc,
            entity.MagicAttack,
            entity.MagicDefense,
            entity.Movement);
    }

    public static CombatantRuntimeState ToDomainRuntimeState(CombatantRuntimeStateEntity entity)
    {
        return CombatantRuntimeState.Rehydrate(
            entity.Id,
            entity.CurrentVitality,
            entity.CurrentGuard,
            entity.CurrentFocus,
            entity.CurrentMana,
            entity.CurrentCharge,
            entity.UpdatedAtUtc,
            entity.ThreatValue,
            entity.LastAttackerId,
            maxMana: entity.MaxMana,
            tookPowerfulHitSinceLastAction: entity.TookPowerfulHitSinceLastAction);
    }

    public static CombatantSkill ToDomain(CombatantSkillEntity entity)
    {
        var tags = string.IsNullOrEmpty(entity.Tags)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(entity.Tags) ?? Array.Empty<string>();

        var statusEffects = string.IsNullOrEmpty(entity.StatusEffectsJson)
            ? null
            : JsonSerializer.Deserialize<SkillStatusEffectSpec[]>(entity.StatusEffectsJson);

        return CombatantSkill.Rehydrate(
            entity.Key,
            entity.DisplayName,
            entity.SkillType,
            entity.TargetingType,
            entity.EffectType,
            entity.ManaCost,
            entity.ChargeCost,
            entity.BasePower,
            tags,
            statusEffects,
            category: entity.Category,
            basePowerIsPercentOfMaxVitality: entity.BasePowerIsPercentOfMaxVitality,
            tacticalRange: entity.TacticalRange,
            tacticalAreaShape: Enum.Parse<Domain.Combats.Tactical.TacticalAreaShape>(entity.TacticalAreaShape),
            requiresLineOfSight: entity.RequiresLineOfSight,
            cooldown: entity.Cooldown,
            isUltimate: entity.IsUltimate,
            emotionalRegister: entity.EmotionalRegister);
    }
}
