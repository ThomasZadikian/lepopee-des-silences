namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogEmotionalRegisterDefinition(
    string Code,
    string DisplayName,
    string Glyph,
    string Color,
    IReadOnlyCollection<CatalogBaseEmotionalAffinity> IncomingAffinities);

public sealed record CatalogBaseEmotionalAffinity(
    string IncomingRegister,
    string Outcome,
    double Multiplier);

public sealed record CatalogEmotionalRegisterCatalog(
    string Version,
    IReadOnlyCollection<CatalogEmotionalRegisterDefinition> Definitions);
