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
}
