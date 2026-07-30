using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Entities;

namespace Leds.GameEngine.Infrastructure.Persistence.Mappers;

public static class PlayerRuntimeStatePersistenceMapper
{
    public static PlayerRuntimeStateEntity ToEntity(PlayerRuntimeState state, Guid runId)
    {
        return new PlayerRuntimeStateEntity
        {
            RunId = runId,
            MaxVitality = state.MaxVitality,
            CurrentVitality = state.CurrentVitality,
            Guard = state.Guard,
            Mana = state.Mana,
            MaxMana = state.MaxMana,
            Charge = state.Charge,
            Skills = state.Skills.Select(s => ToEntity(s, runId)).ToList()
        };
    }

    public static PlayerRuntimeSkillEntity ToEntity(PlayerRuntimeSkill skill, Guid runId)
    {
        return new PlayerRuntimeSkillEntity
        {
            RunId = runId,
            Key = skill.Key,
            DisplayName = skill.DisplayName,
            SkillType = skill.SkillType,
            TargetingType = skill.TargetingType,
            EffectType = skill.EffectType,
            ManaCost = skill.ManaCost,
            ChargeCost = skill.ChargeCost,
            BasePower = skill.BasePower,
            Category = skill.Category,
            BasePowerIsPercentOfMaxVitality = skill.BasePowerIsPercentOfMaxVitality,
            TacticalRange = skill.TacticalRange,
            TacticalAreaShape = skill.TacticalAreaShape,
            RequiresLineOfSight = skill.RequiresLineOfSight,
            Cooldown = skill.Cooldown,
            IsUltimate = skill.IsUltimate,
            EmotionalRegister = skill.EmotionalRegister
        };
    }

    public static PlayerRuntimeState ToDomain(PlayerRuntimeStateEntity entity)
    {
        return PlayerRuntimeState.Rehydrate(
            entity.MaxVitality,
            entity.CurrentVitality,
            entity.Guard,
            entity.Mana,
            entity.Charge,
            entity.Skills.Select(ToDomain).ToList(),
            maxMana: entity.MaxMana);
    }

    public static PlayerRuntimeSkill ToDomain(PlayerRuntimeSkillEntity entity)
    {
        return PlayerRuntimeSkill.Rehydrate(
            entity.Key,
            entity.DisplayName,
            entity.SkillType,
            entity.TargetingType,
            entity.EffectType,
            entity.ManaCost,
            entity.ChargeCost,
            entity.BasePower,
            entity.Category,
            entity.BasePowerIsPercentOfMaxVitality,
            entity.TacticalRange,
            entity.TacticalAreaShape,
            entity.RequiresLineOfSight,
            entity.Cooldown,
            entity.IsUltimate,
            entity.EmotionalRegister);
    }
}
