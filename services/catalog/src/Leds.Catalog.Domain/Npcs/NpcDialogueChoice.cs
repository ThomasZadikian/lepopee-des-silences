namespace Leds.Catalog.Domain.Npcs;

// NextNodeKey null = the choice resolves its consequences and ends the encounter.
public sealed record NpcDialogueChoice(
    string Key,
    string Label,
    IReadOnlyList<DialogueRequirement> Requirements,
    IReadOnlyList<DialogueConsequence> Consequences,
    string? NextNodeKey = null);