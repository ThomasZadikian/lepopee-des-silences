using Leds.Catalog.Domain.Skills.Definitions;

namespace Leds.Catalog.Application.Skills.Definitions.Dtos;

public sealed record SkillDefinitionDto(
    Guid Id, string Key, string Name, string Description, string Version, string Status,
    string SkillType, string TargetingType, string EffectType,
    int ManaCost, int ChargeCost, int BasePower,
    string? EffectKind, string? EffectStatusKey,
    int EffectMagnitude, int EffectDurationTicks, int EffectTickInterval, string? EffectStat)
{
    public static SkillDefinitionDto FromDomain(ISkillDefinition d) => new(
        d.Id.Value, d.Key.Value, d.Name.Value, d.Description.Value, d.Version.Value, d.Status.ToString(),
        d.SkillType, d.TargetingType, d.EffectType, d.ManaCost, d.ChargeCost, d.BasePower,
        d.EffectKind, d.EffectStatusKey, d.EffectMagnitude, d.EffectDurationTicks, d.EffectTickInterval, d.EffectStat);
}