public sealed record CatalogSkillDefinition(
    string Key, string DisplayName, string Description, string SkillType, string TargetingType,
    string EffectType, int ManaCost, int ChargeCost, int BasePower,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<CatalogSkillEffectSpec>? Effects = null,
    string Category = "Physical",
    bool BasePowerIsPercentOfMaxVitality = false,
    int TacticalRange = 1,
    string TacticalAreaShape = "Single",
    bool RequiresLineOfSight = false,
    int Cooldown = 0,
    bool IsUltimate = false,
    string EmotionalRegister = "Neutral");

public sealed record CatalogSkillEffectSpec(
    string Kind,
    string? StatusKey,
    int Magnitude,
    int DurationTicks,
    int TickInterval,
    string? Stat,
    bool MagnitudeIsPercentOfMax,
    bool MagnitudeIsPercentOfBaseStat = false,
    bool AppliesToActor = false,
    bool IsPermanent = false);
