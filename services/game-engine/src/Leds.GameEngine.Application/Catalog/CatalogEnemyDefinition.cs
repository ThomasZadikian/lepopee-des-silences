namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogEnemyDefinition(
    string Key,
    string DisplayName,
    string Description,
    string Archetype,
    IReadOnlyCollection<string> CompatibleRoomTypes,
    int BaseDifficulty,
    int MinRiskLevel,
    int MaxRiskLevel,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> SkillKeys);