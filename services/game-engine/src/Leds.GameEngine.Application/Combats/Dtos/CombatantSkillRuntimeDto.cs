using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatantSkillRuntimeDto(
    string Key,
    string DisplayName,
    string SkillType,
    string TargetingType,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower,
    IReadOnlyCollection<string> Tags,
    string Category = "Physical",
    // The skill's OWN "élément" (registre émotionnel) — null for basic attacks and
    // any skill without a declared type (see EmotionalTypeProfileProvider.SkillTypesByKey).
    string? EmotionalType = null,
    int TacticalRange = 1,
    string TacticalAreaShape = "Single",
    bool RequiresLineOfSight = false,
    int Cooldown = 0,
    bool IsUltimate = false)
{
    public static CombatantSkillRuntimeDto FromDomain(CombatantSkill skill)
    {
        return new CombatantSkillRuntimeDto(
            Key: skill.Key,
            DisplayName: skill.DisplayName,
            SkillType: skill.SkillType,
            TargetingType: skill.TargetingType,
            EffectType: skill.EffectType,
            ManaCost: skill.ManaCost,
            ChargeCost: skill.ChargeCost,
            BasePower: skill.BasePower,
            Tags: skill.Tags,
            Category: skill.Category,
            EmotionalType: skill.EmotionalRegister,
            TacticalRange: skill.TacticalRange,
            TacticalAreaShape: skill.TacticalAreaShape.ToString(),
            RequiresLineOfSight: skill.RequiresLineOfSight,
            Cooldown: skill.Cooldown,
            IsUltimate: skill.IsUltimate);
    }
}
