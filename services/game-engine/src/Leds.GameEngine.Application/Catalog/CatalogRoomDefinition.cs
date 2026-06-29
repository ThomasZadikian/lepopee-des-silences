namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogRoomDefinition(
    string Key, string DisplayName, string Description, string? NarrativeText,
    string RoomFamily, string RoomRarity, string Theme,
    int MinDepth, int MaxDepth, int BaseWeight,
    string? EnemyPoolKey, string? RewardPoolKey, string? LawPoolKey, string? CursePoolKey,
    string? BossDefinitionKey, bool IsUnique);