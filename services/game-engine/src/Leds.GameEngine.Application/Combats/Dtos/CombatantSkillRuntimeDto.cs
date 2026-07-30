using Leds.GameEngine.Application.Combats.Typing;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;

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
    bool RequiresLineOfSight = false)
{
    public static CombatantSkillRuntimeDto FromDomain(CombatantSkill skill)
    {
        var hasType = EmotionalTypeProfileProvider.TryResolveIntrinsicType(skill, out var type);
        var tactical = TacticalSkillProfile.For(skill);

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
            EmotionalType: hasType ? type.ToString() : null,
            TacticalRange: tactical.Range,
            TacticalAreaShape: tactical.AreaShape.ToString(),
            RequiresLineOfSight: tactical.RequiresLineOfSight);
    }
}
