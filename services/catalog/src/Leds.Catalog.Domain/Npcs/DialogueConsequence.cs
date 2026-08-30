namespace Leds.Catalog.Domain.Npcs;

// Flat, serialization-friendly discriminated consequence. `WhenWoundState` is the
// state-conditioning that makes "drink the water" flip meaning (the Majordome).
// Combat branches nest recursively (resolved when the unique combat ends).
// PersistReputationMilestone reuses MemoryFlag as the milestone key — it behaves like
// SetMemoryFlag but additionally persists the milestone to the player's lifetime record.
public sealed record DialogueConsequence(
    ConsequenceKind Kind,
    WoundState? WhenWoundState = null,
    string? NarrativeFragmentKey = null,
    string? RewardCursePoolKey = null,
    string? EncounterKey = null,
    int RelationshipDelta = 0,
    string? MemoryFlag = null,
    string? WoundKey = null,
    string? OfferingKey = null,
    IReadOnlyList<DialogueConsequence>? OnWin = null,
    IReadOnlyList<DialogueConsequence>? OnFlee = null,
    IReadOnlyList<DialogueConsequence>? OnLose = null);