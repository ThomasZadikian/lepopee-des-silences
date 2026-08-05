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

    int TacticalRange { get; }

    string TacticalAreaShape { get; }

    bool RequiresLineOfSight { get; }

    int Cooldown { get; }

    bool IsUltimate { get; }

    string EmotionalRegister { get; }

    /// <summary>Who this skill is meant for: "Player", "Enemy" or "Any". Filters the
    /// player-facing Grimoire so enemy-exclusive skills never surface there, and lets
    /// enemy AI skill pools keep drawing from the same shared skill table unaffected.</summary>
    string Audience { get; }

    /// <summary>Player archetypes allowed to equip this skill. Empty = unrestricted
    /// (any archetype). Only meaningful for Audience "Player"/"Any" skills.</summary>
    IReadOnlyList<string> AllowedArchetypes { get; }

    IReadOnlyList<SkillEffectSpec> Effects { get; }

    bool BasePowerIsPercentOfMaxVitality { get; }
}
