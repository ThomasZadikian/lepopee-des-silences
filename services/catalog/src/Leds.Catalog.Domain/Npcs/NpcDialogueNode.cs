namespace Leds.Catalog.Domain.Npcs;

public sealed record NpcDialogueNode(
    string Key,
    string Speaker,
    IReadOnlyList<string> Lines,
    IReadOnlyList<NpcDialogueChoice> Choices,
    IReadOnlyList<string>? TenseLines = null,
    IReadOnlyList<string>? RupturedLines = null);