using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Runs;

public sealed class PlayerRuntimeSkill
{
    private PlayerRuntimeSkill(
        string key,
        string displayName,
        string skillType,
        string targetingType,
        string effectType,
        int manaCost,
        int chargeCost,
        int basePower,
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
    public string Category { get; }

    public static PlayerRuntimeSkill Create(
        string key,
        string displayName,
        string skillType,
        string targetingType,
        string effectType,
        int manaCost,
        int chargeCost,
        int basePower,
        string category = "Physical")
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Player skill key is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Player skill display name is required.");

        if (basePower < 0)
            throw new DomainException("Player skill base power must be non-negative.");

        return new PlayerRuntimeSkill(
            key.Trim(), displayName.Trim(), skillType, targetingType, effectType, manaCost, chargeCost, basePower,
            string.IsNullOrWhiteSpace(category) ? "Physical" : category);
    }

    /// <summary>
    /// Rehydrates a player runtime skill from a trusted persistence snapshot.
    /// This method must not be used to create a new gameplay skill.
    /// </summary>
    public static PlayerRuntimeSkill Rehydrate(
        string key,
        string displayName,
        string skillType,
        string targetingType,
        string effectType,
        int manaCost,
        int chargeCost,
        int basePower,
        string category = "Physical")
    {
        return new PlayerRuntimeSkill(key, displayName, skillType, targetingType, effectType, manaCost, chargeCost, basePower, category);
    }
}
