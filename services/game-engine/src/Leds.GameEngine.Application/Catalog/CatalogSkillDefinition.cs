public sealed record CatalogSkillDefinition(
    string Key, string DisplayName, string Description, string SkillType, string TargetingType,
    string EffectType, int ManaCost, int ChargeCost, int BasePower,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<CatalogSkillEffectSpec>? Effects = null);

public sealed record CatalogSkillEffectSpec(
    string Kind,
    string? StatusKey,
    int Magnitude,
    int DurationTicks,
    int TickInterval,
    string? Stat,
    bool MagnitudeIsPercentOfMax);
