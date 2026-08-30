namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogEnemyDefinitionSnapshot(
    string Key,
    string Version,
    string DisplayName,
    string Description,
    string? NarrativeText,
    string Archetype,
    string? Family,
    string Rank,
    string? Role,
    int BaseDifficulty,
    int EncounterWeight,
    int MinDepth,
    int MaxDepth,
    bool IsBoss,
    bool IsElite,
    string? RewardProfileKey,
    IReadOnlyCollection<string> Tags,
    CatalogEnemyStatBlockSnapshot StatBlock,
    IReadOnlyCollection<CatalogEnemySkillSnapshot> Skills);
