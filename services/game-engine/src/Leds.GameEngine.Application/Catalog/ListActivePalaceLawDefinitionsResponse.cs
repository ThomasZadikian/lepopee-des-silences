namespace Leds.GameEngine.Application.Catalog;

public sealed record PalaceLawDefinitionView(
    string Key,
    string Name,
    string Description,
    string Rarity,
    string Polarity,
    bool IsMajeure,
    IReadOnlyCollection<string> ImpactDomains);

public sealed record ListActivePalaceLawDefinitionsResponse(IReadOnlyCollection<PalaceLawDefinitionView> Laws);
