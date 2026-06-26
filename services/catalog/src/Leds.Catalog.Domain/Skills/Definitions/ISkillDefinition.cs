using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.Skills.Definitions;

public interface ISkillDefinition : ICatalogContent
{
    string SkillType { get; }

    string TargetingType { get; }

    string EffectType { get; }

    int ManaCost { get; }

    int ChargeCost { get; }

    int BasePower { get; }

    string? EffectKind { get; }
    string? EffectStatusKey { get; }
    int EffectMagnitude { get; }
    int EffectDurationTicks { get; }
    int EffectTickInterval { get; }
    string? EffectStat { get; }
}
