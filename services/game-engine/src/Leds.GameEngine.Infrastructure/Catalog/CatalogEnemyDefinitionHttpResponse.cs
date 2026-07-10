namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed record CatalogEnemyDefinitionHttpResponse(
    string Key,
    string Name,
    string Description,
    string Archetype,
    IReadOnlyCollection<string> CompatibleRoomTypes,
    int BaseDifficulty,
    int MinRiskLevel,
    int MaxRiskLevel,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> SkillKeys,
    int AttackPower = 0,
    int Defense = 0,
    int Speed = 10,
    int Focus = 0);