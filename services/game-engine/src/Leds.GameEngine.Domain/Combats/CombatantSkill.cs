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
        string category,
        bool basePowerIsPercentOfMaxVitality,
        int tacticalRange,
        Tactical.TacticalAreaShape tacticalAreaShape,
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
        Tags = tags;
        StatusEffects = statusEffects;
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
    public IReadOnlyCollection<string> Tags { get; }
    /// <summary>Durable statuses this skill applies to its targets (empty = none). A skill
    /// may carry several simultaneously (e.g. heal-over-time + guard-over-time).</summary>
    public IReadOnlyCollection<SkillStatusEffectSpec> StatusEffects { get; }
    /// <summary>Physical|Magic — determines eligibility for category-scoped combat bonuses
    /// (e.g. Pomenian's "Connaissance académique").</summary>
    public string Category { get; }
    /// <summary>When true (EffectType "Heal" only), BasePower is a percentage of the
    /// target's MaxVitality applied instantly, not a flat amount — e.g. Mané's
    /// "Favorite de Elise" (+15% PV instantly).</summary>
    public bool BasePowerIsPercentOfMaxVitality { get; }
    public int TacticalRange { get; }
    public Tactical.TacticalAreaShape TacticalAreaShape { get; }
    public bool RequiresLineOfSight { get; }
    public int Cooldown { get; }
    public bool IsUltimate { get; }
    public string EmotionalRegister { get; }

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
        string category = "Physical",
        bool basePowerIsPercentOfMaxVitality = false,
        int tacticalRange = 1,
        Tactical.TacticalAreaShape tacticalAreaShape = Tactical.TacticalAreaShape.Single,
        bool requiresLineOfSight = false,
        int cooldown = 0,
        bool isUltimate = false,
        string emotionalRegister = "Neutral")
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

        if (tacticalRange < 0)
            throw new DomainException("Combatant skill tactical range must be non-negative.");

        if (cooldown < 0)
            throw new DomainException("Combatant skill cooldown must be non-negative.");

        if (string.IsNullOrWhiteSpace(emotionalRegister))
            throw new DomainException("Combatant skill emotional register is required.");

        EmotionalTypeCode.ParseRequired(emotionalRegister, $"Combatant skill '{key}' EmotionalRegister");

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
        string category = "Physical",
        bool basePowerIsPercentOfMaxVitality = false,
        int tacticalRange = 1,
        Tactical.TacticalAreaShape tacticalAreaShape = Tactical.TacticalAreaShape.Single,
        bool requiresLineOfSight = false,
        int cooldown = 0,
        bool isUltimate = false,
        string emotionalRegister = "Neutral")
    {
        EmotionalTypeCode.ParseRequired(emotionalRegister, $"Persisted combatant skill '{key}' EmotionalRegister");
        return new CombatantSkill(
            key, displayName, skillType, targetingType, effectType, manaCost, chargeCost,
            basePower, tags, statusEffects ?? Array.Empty<SkillStatusEffectSpec>(), category,
            basePowerIsPercentOfMaxVitality, tacticalRange, tacticalAreaShape,
            requiresLineOfSight, cooldown, isUltimate, emotionalRegister);
    }

    public CombatantSkill WithPowerMultiplier(double multiplier)
    {
        if (multiplier <= 0)
            throw new DomainException("Combatant skill power multiplier must be positive.");

        var scaledStatuses = StatusEffects
            .Select(spec => spec with
            {
                Magnitude = (int)Math.Round(
                    spec.Magnitude * multiplier,
                    MidpointRounding.AwayFromZero)
            })
            .ToArray();

        return new CombatantSkill(
            Key,
            DisplayName,
            SkillType,
            TargetingType,
            EffectType,
            ManaCost,
            ChargeCost,
            (int)Math.Round(BasePower * multiplier, MidpointRounding.AwayFromZero),
            Tags,
            scaledStatuses,
            Category,
            BasePowerIsPercentOfMaxVitality,
            TacticalRange,
            TacticalAreaShape,
            RequiresLineOfSight,
            Cooldown,
            IsUltimate,
            EmotionalRegister);
    }

    public CombatantSkill WithoutResourceCosts() => new(
        Key,
        DisplayName,
        SkillType,
        TargetingType,
        EffectType,
        0,
        0,
        BasePower,
        Tags,
        StatusEffects,
        Category,
        BasePowerIsPercentOfMaxVitality,
        TacticalRange,
        TacticalAreaShape,
        RequiresLineOfSight,
        0,
        false,
        EmotionalRegister);
}
