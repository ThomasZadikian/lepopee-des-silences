namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogRoomEnemyPoolEntrySnapshot(
    string EnemyDefinitionKey,
    int Weight,
    int? MinDepth,
    int? MaxDepth,
    string? RequiredTag,
    string? ExcludedTag);
