namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogEnemySkillSnapshot(
    string SkillDefinitionKey,
    string DisplayName,
    string SkillType,
    string TargetingMode,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower);
