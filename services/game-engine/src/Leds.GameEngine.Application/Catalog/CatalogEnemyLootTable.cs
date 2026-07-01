namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogEnemyLootTable(
    string Key,
    string EnemyDefinitionKey,
    string DisplayName,
    string Description,
    string Version,
    IReadOnlyCollection<CatalogLootEntry> Entries);

public sealed record CatalogGenericLootPool(
    string Key,
    string DisplayName,
    string Description,
    string Version,
    IReadOnlyCollection<CatalogLootEntry> Entries);

public sealed record CatalogLootEntry(
    string ItemDefinitionKey,
    int DropPercent);
