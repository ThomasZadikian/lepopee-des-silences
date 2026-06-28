namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogNpcDialogueGraph(
    string Key,
    string Version,
    string EntryNodeKey,
    IReadOnlyDictionary<string, CatalogNpcDialogueNode> Nodes);