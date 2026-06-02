using Leds.Catalog.Domain.Skills;

namespace Leds.Catalog.Application.Skills.Dtos;

public sealed record SkillTemplateDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string Element,
    string EffectType,
    string TargetType,
    int ManaCost,
    int ChargeCost,
    int BasePower,
    int HealPower)
{
    public static SkillTemplateDto FromDomain(ISkillTemplate skill)
    {
        return new SkillTemplateDto(
            skill.Id.Value,
            skill.Key.Value,
            skill.Name.Value,
            skill.Description.Value,
            skill.Version.Value,
            skill.Status.ToString(),
            skill.Element.ToString(),
            skill.EffectType.ToString(),
            skill.TargetType.ToString(),
            skill.ManaCost,
            skill.ChargeCost,
            skill.BasePower,
            skill.HealPower);
    }
}