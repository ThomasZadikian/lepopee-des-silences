namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogEffectSetSnapshot(
    string Key,
    string Version,
    IReadOnlyCollection<CatalogEffectDefinitionSnapshot> Effects);
