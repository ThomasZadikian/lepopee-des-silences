namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogItemTypeDefinition(
    string Code,
    string DisplayName,
    string Glyph,
    string Color);

public sealed record CatalogItemTypeCatalog(
    string Version,
    IReadOnlyCollection<CatalogItemTypeDefinition> Definitions);

public sealed record CatalogItemRarityDefinition(
    string Code,
    string DisplayName,
    string Glyph,
    string Color,
    int PalaceShardCost,
    int HimLitShardCost);

public sealed record CatalogItemRarityCatalog(
    string Version,
    IReadOnlyCollection<CatalogItemRarityDefinition> Definitions);
