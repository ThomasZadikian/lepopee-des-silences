using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

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
    // Catalog-authored register. Neutral is explicit; the client never derives a
    // second type from keys, tags or the caster.
    string EmotionalRegister = "neutral",
    int TacticalRange = 1,
    string TacticalAreaShape = "Single",
    bool RequiresLineOfSight = false,
    int Cooldown = 0,
    bool IsUltimate = false,
    int? EffectiveManaCost = null)
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
            EmotionalRegister: EmotionalTypeCode.ParseRequired(
                skill.EmotionalRegister,
                $"Skill '{skill.Key}' EmotionalRegister").ToString().ToLowerInvariant(),
            TacticalRange: skill.TacticalRange,
            TacticalAreaShape: skill.TacticalAreaShape.ToString(),
            RequiresLineOfSight: skill.RequiresLineOfSight,
            Cooldown: skill.Cooldown,
            IsUltimate: skill.IsUltimate);
    }
}
