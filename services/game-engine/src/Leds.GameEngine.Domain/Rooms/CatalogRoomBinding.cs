namespace Leds.GameEngine.Domain.Rooms;

public sealed record CatalogRoomBinding(
    string Key, string DisplayName, string? NarrativeText,
    string? EnemyPoolKey, string? RewardPoolKey, string? LawPoolKey, string? CursePoolKey,
    bool IsUnique);