using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Combats.Typing;

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
        string category,
        bool basePowerIsPercentOfMaxVitality,
        int tacticalRange,
        string tacticalAreaShape,
        bool requiresLineOfSight,
        int cooldown,
        bool isUltimate,
        string emotionalRegister)
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
        BasePowerIsPercentOfMaxVitality = basePowerIsPercentOfMaxVitality;
        TacticalRange = tacticalRange;
        TacticalAreaShape = tacticalAreaShape;
        RequiresLineOfSight = requiresLineOfSight;
        Cooldown = cooldown;
        IsUltimate = isUltimate;
        EmotionalRegister = emotionalRegister;
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
    /// <summary>When true (EffectType "Heal" only), BasePower is a percentage of the
    /// target's MaxVitality applied instantly — e.g. Mané's "Favorite de Elise".</summary>
    public bool BasePowerIsPercentOfMaxVitality { get; }
    public int TacticalRange { get; }
    public string TacticalAreaShape { get; }
    public bool RequiresLineOfSight { get; }
    public int Cooldown { get; }
    public bool IsUltimate { get; }
    public string EmotionalRegister { get; }

    public static PlayerRuntimeSkill Create(
        string key,
        string displayName,
        string skillType,
        string targetingType,
        string effectType,
        int manaCost,
        int chargeCost,
        int basePower,
        string category = "Physical",
        bool basePowerIsPercentOfMaxVitality = false,
        int tacticalRange = 1,
        string tacticalAreaShape = "Single",
        bool requiresLineOfSight = false,
        int cooldown = 0,
        bool isUltimate = false,
        string emotionalRegister = null!)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Player skill key is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Player skill display name is required.");

        if (basePower < 0)
            throw new DomainException("Player skill base power must be non-negative.");

        if (tacticalRange < 0)
            throw new DomainException("Player skill tactical range must be non-negative.");

        if (cooldown < 0)
            throw new DomainException("Player skill cooldown must be non-negative.");

        EmotionalTypeCode.ParseRequired(emotionalRegister, $"Player skill '{key}' EmotionalRegister");

        return new PlayerRuntimeSkill(
            key.Trim(), displayName.Trim(), skillType, targetingType, effectType, manaCost, chargeCost, basePower,
            string.IsNullOrWhiteSpace(category) ? "Physical" : category,
            basePowerIsPercentOfMaxVitality,
            tacticalRange,
            tacticalAreaShape,
            requiresLineOfSight,
            cooldown,
            isUltimate,
            emotionalRegister.Trim());
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
        string category = "Physical",
        bool basePowerIsPercentOfMaxVitality = false,
        int tacticalRange = 1,
        string tacticalAreaShape = "Single",
        bool requiresLineOfSight = false,
        int cooldown = 0,
        bool isUltimate = false,
        string emotionalRegister = null!)
    {
        EmotionalTypeCode.ParseRequired(emotionalRegister, $"Persisted player skill '{key}' EmotionalRegister");
        return new PlayerRuntimeSkill(
            key, displayName, skillType, targetingType, effectType, manaCost, chargeCost,
            basePower, category, basePowerIsPercentOfMaxVitality, tacticalRange,
            tacticalAreaShape, requiresLineOfSight, cooldown, isUltimate,
            emotionalRegister);
    }
}
