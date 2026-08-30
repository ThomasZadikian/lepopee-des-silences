namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogNpcDialogueChoice(
    string Key,
    string Label,
    IReadOnlyCollection<CatalogDialogueRequirement> Requirements,
    IReadOnlyCollection<CatalogDialogueConsequence> Consequences,
    string? NextNodeKey);