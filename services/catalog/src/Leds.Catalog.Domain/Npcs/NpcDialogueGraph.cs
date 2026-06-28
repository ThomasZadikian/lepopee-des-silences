namespace Leds.Catalog.Domain.Npcs;

// Free-depth dialogue graph (decision Q1). One jsonb document per PNJ, read whole
// once per encounter then cached. Nodes keyed by their node key.
public sealed record NpcDialogueGraph(
    string Key,
    string Version,
    string EntryNodeKey,
    IReadOnlyDictionary<string, NpcDialogueNode> Nodes);