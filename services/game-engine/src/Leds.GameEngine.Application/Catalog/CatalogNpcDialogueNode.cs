namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogNpcDialogueNode(
    string Key,
    string Speaker,
    IReadOnlyCollection<string> Lines,
    IReadOnlyCollection<CatalogNpcDialogueChoice> Choices);