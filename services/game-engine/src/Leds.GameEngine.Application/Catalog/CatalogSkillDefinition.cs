public sealed record CatalogSkillDefinition(
    string Key, string DisplayName, string Description, string SkillType, string TargetingType,
    string EffectType, int ManaCost, int ChargeCost, int BasePower,
    IReadOnlyCollection<string> Tags,
    string? EffectKind = null, string? EffectStatusKey = null,
    int EffectMagnitude = 0, int EffectDurationTicks = 0, int EffectTickInterval = 0, string? EffectStat = null);