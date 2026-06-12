using System.Text.Json;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Entities;

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
            Skills = combatant.Skills.Select(s => ToEntity(s, combatant.Id.Value)).ToList()
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
        var allies = combatants.Where(c => c.Side == CombatantSide.Player).ToList();
        var enemies = combatants.Where(c => c.Side == CombatantSide.Enemy).ToList();

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
            entity.CreatedAtUtc);
    }

    public static Combatant ToDomain(CombatantEntity entity)
    {
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
            entity.Skills.Select(ToDomain).ToList());
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
