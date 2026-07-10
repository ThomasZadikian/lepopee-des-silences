using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats;

public sealed record CombatantSkill
{
    private CombatantSkill(
        string key,
        string displayName,
        string skillType,
        string targetingType,
        string effectType,
        int manaCost,
        int chargeCost,
        int basePower,
        IReadOnlyCollection<string> tags,
        IReadOnlyCollection<SkillStatusEffectSpec> statusEffects,
        string category)
    {
        Key = key;
        DisplayName = displayName;
        SkillType = skillType;
        TargetingType = targetingType;
        EffectType = effectType;
        ManaCost = manaCost;
        ChargeCost = chargeCost;
        BasePower = basePower;
        Tags = tags;
        StatusEffects = statusEffects;
        Category = category;
    }

    public string Key { get; }
    public string DisplayName { get; }
    public string SkillType { get; }
    public string TargetingType { get; }
    public string EffectType { get; }
    public int ManaCost { get; }
    public int ChargeCost { get; }
    public int BasePower { get; }
    public IReadOnlyCollection<string> Tags { get; }
    /// <summary>Durable statuses this skill applies to its targets (empty = none). A skill
    /// may carry several simultaneously (e.g. heal-over-time + guard-over-time).</summary>
    public IReadOnlyCollection<SkillStatusEffectSpec> StatusEffects { get; }
    /// <summary>Physical|Magic — determines eligibility for category-scoped combat bonuses
    /// (e.g. Pomenian's "Connaissance académique").</summary>
    public string Category { get; }

    public static CombatantSkill Create(
        string key,
        string displayName,
        string skillType,
        string targetingType,
        string effectType,
        int manaCost,
        int chargeCost,
        int basePower,
        IReadOnlyCollection<string>? tags = null,
        IReadOnlyCollection<SkillStatusEffectSpec>? statusEffects = null,
        string category = "Physical")
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Combatant skill key is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Combatant skill display name is required.");

        if (basePower < 0)
            throw new DomainException("Combatant skill base power must be non-negative.");

        if (manaCost < 0)
            throw new DomainException("Combatant skill mana cost must be non-negative.");

        if (chargeCost < 0)
            throw new DomainException("Combatant skill charge cost must be non-negative.");

        return new CombatantSkill(
            key.Trim(),
            displayName.Trim(),
            skillType,
            targetingType,
            effectType,
            manaCost,
            chargeCost,
            basePower,
            tags?.ToArray() ?? Array.Empty<string>(),
            statusEffects?.ToArray() ?? Array.Empty<SkillStatusEffectSpec>(),
            string.IsNullOrWhiteSpace(category) ? "Physical" : category);
    }

    /// <summary>
    /// Rehydrates a combatant skill from a trusted persistence snapshot.
    /// This method must not be used to create a new gameplay skill.
    /// </summary>
    public static CombatantSkill Rehydrate(
        string key,
        string displayName,
        string skillType,
        string targetingType,
        string effectType,
        int manaCost,
        int chargeCost,
        int basePower,
        IReadOnlyCollection<string> tags,
        IReadOnlyCollection<SkillStatusEffectSpec>? statusEffects = null,
        string category = "Physical")
    {
        return new CombatantSkill(key, displayName, skillType, targetingType, effectType, manaCost, chargeCost, basePower, tags, statusEffects ?? Array.Empty<SkillStatusEffectSpec>(), category);
    }
}
