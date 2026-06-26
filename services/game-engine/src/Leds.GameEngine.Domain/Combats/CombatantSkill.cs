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
        SkillStatusEffectSpec? statusEffect)
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
        StatusEffect = statusEffect;
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
    /// <summary>Optional durable status this skill applies to its targets (null = none).</summary>
    public SkillStatusEffectSpec? StatusEffect { get; }

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
        SkillStatusEffectSpec? statusEffect = null)
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
            statusEffect);
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
        SkillStatusEffectSpec? statusEffect = null)
    {
        return new CombatantSkill(key, displayName, skillType, targetingType, effectType, manaCost, chargeCost, basePower, tags, statusEffect);
    }
}