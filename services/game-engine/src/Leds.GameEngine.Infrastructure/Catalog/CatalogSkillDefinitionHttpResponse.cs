namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed record CatalogSkillDefinitionHttpResponse(
    string Key,
    string Name,
    string Description,
    string SkillType,
    string TargetingType,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower,
    IReadOnlyCollection<string> Tags);
