namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogRoomDefinition(
    string Key, string DisplayName, string Description, string? NarrativeText,
    string RoomFamily, string RoomRarity, string Theme,
    int MinDepth, int MaxDepth, int BaseWeight,
    string? EnemyPoolKey, string? RewardPoolKey, string? LawPoolKey, string? CursePoolKey,
    string? BossDefinitionKey, bool IsUnique,
    string? WorldKey, bool IsWorldEntryRoom, bool TriggersStrictChain,
    IReadOnlyCollection<string> ReachableRoomKeys);

public sealed record CatalogWorldDefinition(string Key, string DisplayName, string EntryRoomKey);

public sealed record CatalogRoomThemeAffinity(string ThemeFrom, string ThemeTo, decimal Weight);