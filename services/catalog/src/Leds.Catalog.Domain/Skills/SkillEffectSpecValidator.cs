using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Gameplay;

namespace Leds.Catalog.Domain.Skills;

public static class SkillEffectSpecValidator
{
    private static readonly IReadOnlySet<string> SupportedKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "DamageOverTime", "HealOverTime", "GuardOverTime", "StatModifier",
        "Stun", "Silence", "SkillGrant", "AffinityModifier"
    };

    private static readonly IReadOnlySet<string> SupportedStats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "AttackPower", "Defense", "Speed", "Movement", "Focus", "Evasion",
        "MagicAttack", "MagicDefense", "MagicDamageBonus", "MagicDamageReduction",
        "CriticalChanceBonus", "DotDamageBonus", "SkillCostReductionPercent",
        "HealingBonus", "PhysicalDamageBonus", "FireDamageBonus", "FlatManaCostBonus"
    };

    public static void Validate(string skillKey, IEnumerable<SkillEffectSpec> effects)
    {
        foreach (var effect in effects)
        {
            if (string.IsNullOrWhiteSpace(effect.Kind))
                throw new DomainException($"Skill '{skillKey}' effect kind is required.");
            if (!SupportedKinds.Contains(effect.Kind))
                throw new DomainException($"Skill '{skillKey}' effect kind '{effect.Kind}' is not supported.");
            if (!effect.IsPermanent && effect.DurationTicks <= 0)
                throw new DomainException($"Skill '{skillKey}' effect duration must be positive.");

            if (string.Equals(effect.Kind, "StatModifier", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(effect.Stat) || !SupportedStats.Contains(effect.Stat))
                    throw new DomainException(
                        $"Skill '{skillKey}' StatModifier requires a supported Stat.");
            }
            else if (!string.IsNullOrWhiteSpace(effect.Stat) && !SupportedStats.Contains(effect.Stat))
            {
                throw new DomainException($"Skill '{skillKey}' effect stat '{effect.Stat}' is not supported.");
            }

            if (!string.Equals(effect.Kind, "AffinityModifier", StringComparison.OrdinalIgnoreCase))
                continue;

            EmotionalRegisterCatalog.Parse(effect.AffinityRegister ?? string.Empty);
            if (string.IsNullOrWhiteSpace(effect.AffinityOutcome) && effect.Magnitude == 0)
                throw new DomainException(
                    $"Skill '{skillKey}' affinity modifier requires an outcome or multiplier.");
            if (!string.IsNullOrWhiteSpace(effect.AffinityOutcome)
                && !Enum.TryParse<AffinityOutcome>(effect.AffinityOutcome, true, out _))
                throw new DomainException($"Skill '{skillKey}' affinity outcome is invalid.");
        }
    }
}
