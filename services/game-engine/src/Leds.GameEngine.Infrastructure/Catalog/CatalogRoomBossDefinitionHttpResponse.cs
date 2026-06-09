namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed record CatalogRoomBossDefinitionHttpResponse(
    string Key,
    string Name,
    string Description,
    string RoomType,
    int BaseDifficulty,
    IReadOnlyCollection<string> Tags);
