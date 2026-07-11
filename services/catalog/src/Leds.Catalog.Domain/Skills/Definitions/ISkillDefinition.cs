using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.Skills;

namespace Leds.Catalog.Domain.Skills.Definitions;

public interface ISkillDefinition : ICatalogContent
{
    string SkillType { get; }

    string TargetingType { get; }

    string EffectType { get; }

    string Category { get; }

    int ManaCost { get; }

    int ChargeCost { get; }

    int BasePower { get; }

    IReadOnlyList<SkillEffectSpec> Effects { get; }

    bool BasePowerIsPercentOfMaxVitality { get; }
}
