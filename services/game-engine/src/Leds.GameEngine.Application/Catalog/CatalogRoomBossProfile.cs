namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogRoomBossProfile(
    string Key,
    string DisplayName,
    string Description,
    string RoomType,
    int BaseDifficulty,
    IReadOnlyCollection<string> Tags);