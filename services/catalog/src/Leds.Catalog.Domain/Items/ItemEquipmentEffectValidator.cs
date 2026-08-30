using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Items;

/// <summary>Single validation registry for every equipment effect supported by the runtime.</summary>
public static class ItemEquipmentEffectValidator
{
    private static readonly IReadOnlySet<string> StatKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MaxVitality", "AttackPower", "Defense", "Guard", "Speed", "Focus",
        "MagicAttack", "MagicDefense", "Mana", "Movement", "RunItemCapacity"
    };
    private static readonly IReadOnlySet<string> RuntimeBehaviorCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "defense-after-no-magic",
        "hostile-status-duration-plus-one",
        "first-dot-duration-plus-one",
        "reflect-first-melee-hit",
        "prevent-revive-signature-on-death",
        "silence-mana-minus-two",
        "silence-duration-plus-one",
        "team-silence-every-five-activations",
        "tactical-extend-periodic-duration",
        "tactical-temporal-slow",
        "tactical-mind-control",
        "known-forge-skill-awards-eight-points",
        "run-journal",
        "deny-palace-law",
        "reputation-gain-plus-ten",
        "himlit-protection",
        "infinite-chalice"
    };

    public static void Validate(string itemKey, IEnumerable<ItemEquipmentEffect> effects)
    {
        ArgumentNullException.ThrowIfNull(effects);

        foreach (var effect in effects)
        {
            var field = $"Item '{itemKey}' effect '{effect.Kind}'";

            if (effect.Condition is not null
                && effect.Kind is not (ItemEquipmentEffectKind.StatBonus
                    or ItemEquipmentEffectKind.StatBonusPercent))
                throw new DomainException($"{field} does not support Condition.");

            switch (effect.Kind)
            {
                case ItemEquipmentEffectKind.StatBonus:
                case ItemEquipmentEffectKind.StatBonusPercent:
                    if (string.IsNullOrWhiteSpace(effect.StatKind) || !StatKinds.Contains(effect.StatKind))
                        throw new DomainException($"{field} requires a supported StatKind.");
                    RequireAmount(field, effect.Amount);
                    ValidateCondition(field, effect.Condition);
                    break;

                case ItemEquipmentEffectKind.GrantSkill:
                    if (string.IsNullOrWhiteSpace(effect.SkillKey))
                        throw new DomainException($"{field} requires SkillKey.");
                    break;

                case ItemEquipmentEffectKind.GrantAffinity:
                    if (effect.AffinityRegister is null)
                        throw new DomainException($"{field} requires AffinityRegister.");
                    break;

                case ItemEquipmentEffectKind.DamageReductionByType:
                    if (effect.AffinityRegister is null)
                        throw new DomainException($"{field} requires AffinityRegister.");
                    RequirePercentage(field, effect.Amount);
                    break;

                case ItemEquipmentEffectKind.DotDurationReduction:
                case ItemEquipmentEffectKind.DotDamageReduction:
                case ItemEquipmentEffectKind.MagicDamageReductionPercent:
                    RequirePercentage(field, effect.Amount);
                    break;

                case ItemEquipmentEffectKind.HitChanceBonus:
                case ItemEquipmentEffectKind.MagicDamageBonusPercent:
                case ItemEquipmentEffectKind.CriticalChanceBonusPercent:
                case ItemEquipmentEffectKind.DotDamageBonusPercent:
                case ItemEquipmentEffectKind.HealingBonusPercent:
                    RequireAmount(field, effect.Amount);
                    break;

                case ItemEquipmentEffectKind.AffinityOutcomeOverride:
                    if (effect.AffinityRegister is null || effect.AffinityOutcome is null)
                        throw new DomainException($"{field} requires AffinityRegister and AffinityOutcome.");
                    ValidateDuration(field, effect.DurationActivations);
                    break;

                case ItemEquipmentEffectKind.AffinityMultiplierPercent:
                    if (effect.AffinityRegister is null)
                        throw new DomainException($"{field} requires AffinityRegister.");
                    RequireAmount(field, effect.Amount);
                    ValidateDuration(field, effect.DurationActivations);
                    break;

                case ItemEquipmentEffectKind.RuntimeBehavior:
                    if (string.IsNullOrWhiteSpace(effect.BehaviorCode)
                        || !RuntimeBehaviorCodes.Contains(effect.BehaviorCode))
                        throw new DomainException($"{field} requires a supported BehaviorCode.");
                    break;

                default:
                    throw new DomainException($"{field} is not supported by the equipment runtime.");
            }
        }
    }

    private static void RequireAmount(string field, int? amount)
    {
        if (amount is null)
            throw new DomainException($"{field} requires Amount.");
    }

    private static void RequirePercentage(string field, int? amount)
    {
        if (amount is null or < 0 or > 100)
            throw new DomainException($"{field} requires Amount between 0 and 100.");
    }

    private static void ValidateDuration(string field, int? duration)
    {
        if (duration is <= 0)
            throw new DomainException($"{field} DurationActivations must be positive when supplied.");
    }

    private static void ValidateCondition(string field, string? condition)
    {
        if (condition is null)
            return;

        if (!condition.StartsWith("room:", StringComparison.OrdinalIgnoreCase)
            && !condition.StartsWith("weather:", StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"{field} condition must start with 'room:' or 'weather:'.");

        if (condition.EndsWith(':'))
            throw new DomainException($"{field} condition value is required.");
    }
}
