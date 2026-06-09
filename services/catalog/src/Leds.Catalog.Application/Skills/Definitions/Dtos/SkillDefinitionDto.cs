using Leds.Catalog.Domain.Skills.Definitions;

namespace Leds.Catalog.Application.Skills.Definitions.Dtos;

public sealed record SkillDefinitionDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string SkillType,
    string TargetingType,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower)
{
    public static SkillDefinitionDto FromDomain(ISkillDefinition definition)
    {
        return new SkillDefinitionDto(
            definition.Id.Value,
            definition.Key.Value,
            definition.Name.Value,
            definition.Description.Value,
            definition.Version.Value,
            definition.Status.ToString(),
            definition.SkillType,
            definition.TargetingType,
            definition.EffectType,
            definition.ManaCost,
            definition.ChargeCost,
            definition.BasePower);
    }
}
