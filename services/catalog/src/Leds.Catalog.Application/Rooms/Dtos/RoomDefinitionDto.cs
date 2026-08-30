namespace Leds.Catalog.Application.Rooms.Dtos;

public sealed record RoomDefinitionDto(
    Guid Id, string Key, string DisplayName, string Description, string? NarrativeText,
    string RoomFamily, string RoomRarity, string Theme,
    int? MinDepth, int? MaxDepth, int BaseWeight, string? SelectionGroup,
    string? EnemyPoolKey, string? RewardPoolKey, string? LawPoolKey, string? CursePoolKey,
    string? SpecialMechanicKey, string? BossDefinitionKey,
    bool IsUnique, bool IsCulturalEcho,
    string? WorldKey, bool IsWorldEntryRoom, bool TriggersStrictChain,
    IReadOnlyCollection<string> ReachableRoomKeys,
    string Version, string Status);