namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogSkillDefinition(
    string Key,
    string DisplayName,
    string Description,
    string SkillType,
    string TargetingType,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower,
    IReadOnlyCollection<string> Tags);
