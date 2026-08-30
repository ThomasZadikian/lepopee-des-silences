namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogRoomEnemyPoolSnapshot(
    string Key,
    string Version,
    IReadOnlyCollection<CatalogRoomEnemyPoolEntrySnapshot> Entries);
