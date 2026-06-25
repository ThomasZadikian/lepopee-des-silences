using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Entities;
using System.Text.Json;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Infrastructure.Persistence.Mappers;

public static class CombatPersistenceMapper
{
    // -----------------------------------------------------------------------
    // Domain → Entity
    // -----------------------------------------------------------------------

    public static CombatEntity ToEntity(Combat combat, Guid runId)
    {
        var allCombatants = combat.Allies.Concat(combat.Enemies);

        return new CombatEntity
        {
            Id = combat.Id.Value,
            RunId = runId,
            RoomId = combat.RoomId.Value,
            NodeId = combat.NodeId.Value,
            Status = combat.Status.ToString(),
            TurnNumber = combat.TurnNumber,
            CurrentTick = combat.CurrentTick,
            ActiveCombatantId = combat.ActiveCombatantId?.Value,
            CreatedAtUtc = combat.CreatedAtUtc,
            UpdatedAtUtc = DateTime.UtcNow,
            Combatants = allCombatants.Select(c => ToEntity(c, combat.Id.Value)).ToList()
        };
    }

    public static CombatantEntity ToEntity(Combatant combatant, Guid combatId)
    {
        return new CombatantEntity
        {
            Id = combatant.Id.Value,
            CombatId = combatId,
            SourceKey = combatant.SourceKey,
            DisplayName = combatant.DisplayName,
            Side = combatant.Side.ToString(),
            Archetype = combatant.Archetype,
            MaxVitality = combatant.MaxVitality,
            CurrentVitality = combatant.CurrentVitality,
            Guard = combatant.Guard,
            BaseGuard = combatant.BaseGuard,
            Mana = combatant.Mana,
            Charge = combatant.Charge,
            Status = combatant.Status.ToString(),
            AttackTypeOverride = combatant.AttackTypeOverride.HasValue ? (int)combatant.AttackTypeOverride.Value : null,
            Skills = combatant.Skills.Select(s => ToEntity(s, combatant.Id.Value)).ToList(),
            BaseStatSnapshot = ToBaseStatSnapshotEntity(combatant.BaseStatSnapshot, combatant.Id.Value),
            RuntimeState = ToRuntimeStateEntity(combatant.RuntimeState, combatant.Id.Value)
        };
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
            Recovery = snapshot.Recovery,
            Focus = snapshot.Focus,
            Mana = snapshot.Mana,
            Charge = snapshot.Charge,
            AtbReadyThreshold = snapshot.AtbReadyThreshold,
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
            CurrentCharge = state.CurrentCharge,
            AtbGaugeValue = state.AtbGaugeValue,
            ActionRecoveryUntilTick = state.ActionRecoveryUntilTick,
            AtbFillPerTick = state.AtbFillPerTick,
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
            Tags = JsonSerializer.Serialize(skill.Tags)
        };
    }

    // -----------------------------------------------------------------------
    // Entity → Domain
    // -----------------------------------------------------------------------

    public static Combat ToDomain(CombatEntity entity)
    {
        var combatants = entity.Combatants.Select(ToDomain).ToList();

        // Deterministic, protagonist-first ordering so the player is ALWAYS first in
        // combat — and stays first across persistence reloads (which happen every
        // tick). Companions/enemies keep a stable order too.
        var allies = combatants
            .Where(c => c.Side == CombatantSide.Player)
            .OrderBy(c => string.Equals(c.SourceKey, "player.self", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(c => c.SourceKey, StringComparer.Ordinal)
            .ThenBy(c => c.Id.Value)
            .ToList();
        var enemies = combatants
            .Where(c => c.Side == CombatantSide.Enemy)
            .OrderBy(c => c.SourceKey, StringComparer.Ordinal)
            .ThenBy(c => c.Id.Value)
            .ToList();

        return Combat.Rehydrate(
            new CombatId(entity.Id),
            new RunId(entity.RunId),
            new RoomId(entity.RoomId),
            new NodeId(entity.NodeId),
            Enum.Parse<CombatStatus>(entity.Status),
            allies,
            enemies,
            entity.ActiveCombatantId.HasValue ? new CombatantId(entity.ActiveCombatantId.Value) : null,
            entity.TurnNumber,
            entity.CreatedAtUtc, 
            entity.CurrentTick            
            );
    }

    public static Combatant ToDomain(CombatantEntity entity)
    {
        var baseStatSnapshot = entity.BaseStatSnapshot is not null
            ? ToDomainBaseStatSnapshot(entity.BaseStatSnapshot)
            : null;

        var runtimeState = entity.RuntimeState is not null
            ? ToDomainRuntimeState(entity.RuntimeState)
            : null;

        return Combatant.Rehydrate(
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
            attackTypeOverride: entity.AttackTypeOverride.HasValue ? (EmotionalType)entity.AttackTypeOverride.Value : null);
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
            entity.Recovery,
            entity.Focus,
            entity.Mana,
            entity.Charge,
            entity.AtbReadyThreshold,
            entity.CreatedAtUtc);
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
            entity.AtbGaugeValue,
            entity.ActionRecoveryUntilTick,
            entity.UpdatedAtUtc,
            entity.AtbFillPerTick);
    }

    public static CombatantSkill ToDomain(CombatantSkillEntity entity)
    {
        var tags = string.IsNullOrEmpty(entity.Tags)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(entity.Tags) ?? Array.Empty<string>();

        return CombatantSkill.Rehydrate(
            entity.Key,
            entity.DisplayName,
            entity.SkillType,
            entity.TargetingType,
            entity.EffectType,
            entity.ManaCost,
            entity.ChargeCost,
            entity.BasePower,
            tags);
    }
}