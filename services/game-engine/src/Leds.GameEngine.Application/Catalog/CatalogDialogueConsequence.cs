namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogDialogueConsequence(
    string Kind,
    string? WhenWoundState,
    string? NarrativeFragmentKey,
    string? RewardCursePoolKey,
    string? EncounterKey,
    int RelationshipDelta,
    string? MemoryFlag,
    string? WoundKey,
    IReadOnlyCollection<CatalogDialogueConsequence>? OnWin,
    IReadOnlyCollection<CatalogDialogueConsequence>? OnFlee,
    IReadOnlyCollection<CatalogDialogueConsequence>? OnLose);