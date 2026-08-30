namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogNpcDialogueNode(
    string Key,
    string Speaker,
    IReadOnlyCollection<string> Lines,
    IReadOnlyCollection<CatalogNpcDialogueChoice> Choices,
    IReadOnlyCollection<string>? TenseLines = null,
    IReadOnlyCollection<string>? RupturedLines = null);