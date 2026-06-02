using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.Combat;

namespace Leds.Catalog.Domain.Skills;

public interface ISkillTemplate : ICatalogContent
{
    CombatElement Element { get; }

    SkillEffectType EffectType { get; }

    SkillTargetType TargetType { get; }

    int ManaCost { get; }

    int ChargeCost { get; }

    int BasePower { get; }

    int HealPower { get; }
}