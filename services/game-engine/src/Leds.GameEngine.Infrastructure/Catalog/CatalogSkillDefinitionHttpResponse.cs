public sealed record CatalogSkillDefinitionHttpResponse(
    string Key, string Name, string Description, string SkillType, string TargetingType,
    string EffectType, int ManaCost, int ChargeCost, int BasePower,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<CatalogSkillEffectSpecHttpResponse>? Effects = null);

public sealed record CatalogSkillEffectSpecHttpResponse(
    string Kind,
    string? StatusKey,
    int Magnitude,
    int DurationTicks,
    int TickInterval,
    string? Stat,
    bool MagnitudeIsPercentOfMax);
