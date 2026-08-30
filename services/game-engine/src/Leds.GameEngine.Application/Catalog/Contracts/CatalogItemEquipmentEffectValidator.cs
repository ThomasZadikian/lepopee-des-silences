using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Application.Catalog.Contracts;

/// <summary>Game Engine-side allow-list for equipment handlers accepted from Catalog.</summary>
public static class CatalogItemEquipmentEffectValidator
{
    private static readonly IReadOnlySet<string> SupportedKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "StatBonus", "StatBonusPercent", "GrantSkill", "GrantAffinity",
        "DamageReductionByType", "HitChanceBonus", "DotDurationReduction",
        "DotDamageReduction", "MagicDamageBonusPercent", "MagicDamageReductionPercent",
        "CriticalChanceBonusPercent", "DotDamageBonusPercent", "HealingBonusPercent",
        "AffinityOutcomeOverride", "AffinityMultiplierPercent", "RuntimeBehavior"
    };
    private static readonly IReadOnlySet<string> SupportedStatKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MaxVitality", "AttackPower", "Defense", "Guard", "Speed", "Focus",
        "MagicAttack", "MagicDefense", "Mana", "Movement", "RunItemCapacity"
    };
    private static readonly IReadOnlySet<string> SupportedRuntimeBehaviors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "defense-after-no-magic", "hostile-status-duration-plus-one",
        "first-dot-duration-plus-one", "reflect-first-melee-hit",
        "prevent-revive-signature-on-death", "silence-mana-minus-two",
        "silence-duration-plus-one", "team-silence-every-five-activations",
        "tactical-extend-periodic-duration", "tactical-temporal-slow",
        "tactical-mind-control", "known-forge-skill-awards-eight-points",
        "run-journal", "deny-palace-law", "reputation-gain-plus-ten",
        "himlit-protection", "infinite-chalice"
    };

    public static void Validate(string itemKey, IEnumerable<CatalogItemEquipmentEffect> effects)
    {
        foreach (var effect in effects)
        {
            if (!SupportedKinds.Contains(effect.Kind))
                throw new DomainException($"Item '{itemKey}' uses unsupported equipment effect '{effect.Kind}'.");

            if (effect.Condition is not null
                && !EqualsAny(effect.Kind, "StatBonus", "StatBonusPercent"))
                throw new DomainException(
                    $"Item '{itemKey}' effect '{effect.Kind}' does not support Condition.");

            if (EqualsAny(effect.Kind, "StatBonus", "StatBonusPercent"))
            {
                if (string.IsNullOrWhiteSpace(effect.StatKind)
                    || !SupportedStatKinds.Contains(effect.StatKind))
                    throw new DomainException($"Item '{itemKey}' effect '{effect.Kind}' has an unsupported StatKind.");
                RequireAmount(itemKey, effect);
                ValidateCondition(itemKey, effect.Condition);
            }

            if (string.Equals(effect.Kind, "GrantSkill", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(effect.SkillKey))
                throw new DomainException($"Item '{itemKey}' GrantSkill effect requires SkillKey.");

            if (effect.Kind.Contains("Affinity", StringComparison.OrdinalIgnoreCase)
                || string.Equals(effect.Kind, "DamageReductionByType", StringComparison.OrdinalIgnoreCase))
            {
                EmotionalTypeCode.ParseRequired(
                    effect.AffinityRegister,
                    $"Item '{itemKey}' effect '{effect.Kind}' AffinityRegister");
            }

            if (string.Equals(effect.Kind, "AffinityOutcomeOverride", StringComparison.OrdinalIgnoreCase)
                && !Enum.TryParse<DamageEffectiveness>(effect.AffinityOutcome, true, out _))
                throw new DomainException($"Item '{itemKey}' affinity outcome is invalid.");

            if (EqualsAny(effect.Kind, "AffinityMultiplierPercent", "HitChanceBonus",
                    "MagicDamageBonusPercent", "CriticalChanceBonusPercent",
                    "DotDamageBonusPercent", "HealingBonusPercent"))
                RequireAmount(itemKey, effect);

            if (EqualsAny(effect.Kind, "DamageReductionByType", "DotDurationReduction",
                    "DotDamageReduction", "MagicDamageReductionPercent"))
            {
                RequireAmount(itemKey, effect);
                if (effect.Amount is < 0 or > 100)
                    throw new DomainException(
                        $"Item '{itemKey}' effect '{effect.Kind}' Amount must be between 0 and 100.");
            }

            if (effect.DurationActivations is <= 0)
                throw new DomainException($"Item '{itemKey}' affinity duration must be positive when supplied.");

            if (string.Equals(effect.Kind, "RuntimeBehavior", StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(effect.BehaviorCode)
                    || !SupportedRuntimeBehaviors.Contains(effect.BehaviorCode)))
                throw new DomainException($"Item '{itemKey}' runtime behavior code is not supported.");
        }
    }

    private static void RequireAmount(string itemKey, CatalogItemEquipmentEffect effect)
    {
        if (effect.Amount is null)
            throw new DomainException($"Item '{itemKey}' effect '{effect.Kind}' requires Amount.");
    }

    private static void ValidateCondition(string itemKey, string? condition)
    {
        if (condition is null)
            return;

        if ((!condition.StartsWith("room:", StringComparison.OrdinalIgnoreCase)
             && !condition.StartsWith("weather:", StringComparison.OrdinalIgnoreCase))
            || condition.EndsWith(':'))
            throw new DomainException(
                $"Item '{itemKey}' condition must be 'room:<value>' or 'weather:<value>'.");
    }

    private static bool EqualsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase));
}
