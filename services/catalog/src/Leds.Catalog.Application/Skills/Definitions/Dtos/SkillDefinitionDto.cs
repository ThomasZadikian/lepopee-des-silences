using Leds.Catalog.Domain.Skills;
using Leds.Catalog.Domain.Skills.Definitions;

namespace Leds.Catalog.Application.Skills.Definitions.Dtos;

public sealed record SkillDefinitionDto(
    Guid Id, string Key, string Name, string Description, string Version, string Status,
    string SkillType, string TargetingType, string EffectType,
    int ManaCost, int ChargeCost, int BasePower,
    IReadOnlyCollection<SkillEffectSpecDto> Effects,
    string Category = "Physical",
    bool BasePowerIsPercentOfMaxVitality = false,
    int TacticalRange = 1,
    string TacticalAreaShape = "Single",
    bool RequiresLineOfSight = false,
    int Cooldown = 0,
    bool IsUltimate = false,
    string EmotionalRegister = "Neutral")
{
    public static SkillDefinitionDto FromDomain(ISkillDefinition d) => new(
        d.Id.Value, d.Key.Value, d.Name.Value, d.Description.Value, d.Version.Value, d.Status.ToString(),
        d.SkillType, d.TargetingType, d.EffectType, d.ManaCost, d.ChargeCost, d.BasePower,
        d.Effects.Select(SkillEffectSpecDto.FromDomain).ToArray(),
        d.Category,
        d.BasePowerIsPercentOfMaxVitality,
        d.TacticalRange,
        d.TacticalAreaShape,
        d.RequiresLineOfSight,
        d.Cooldown,
        d.IsUltimate,
        d.EmotionalRegister);
}

public sealed record SkillEffectSpecDto(
    string Kind,
    string? StatusKey,
    int Magnitude,
    int DurationTicks,
    int TickInterval,
    string? Stat,
    bool MagnitudeIsPercentOfMax,
    bool MagnitudeIsPercentOfBaseStat = false,
    bool AppliesToActor = false,
    bool IsPermanent = false)
{
    public static SkillEffectSpecDto FromDomain(SkillEffectSpec spec) => new(
        spec.Kind, spec.StatusKey, spec.Magnitude, spec.DurationTicks,
        spec.TickInterval, spec.Stat, spec.MagnitudeIsPercentOfMax,
        spec.MagnitudeIsPercentOfBaseStat, spec.AppliesToActor, spec.IsPermanent);
}
