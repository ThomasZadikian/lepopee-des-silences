using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Dtos;

public sealed record DialogueConsequenceDto(
    string Kind,
    string? WhenWoundState,
    string? NarrativeFragmentKey,
    string? RewardCursePoolKey,
    string? EncounterKey,
    int RelationshipDelta,
    string? MemoryFlag,
    string? WoundKey,
    string? OfferingKey,
    IReadOnlyCollection<DialogueConsequenceDto>? OnWin,
    IReadOnlyCollection<DialogueConsequenceDto>? OnFlee,
    IReadOnlyCollection<DialogueConsequenceDto>? OnLose)
{
    public static DialogueConsequenceDto FromDomain(DialogueConsequence c) => new(
        c.Kind.ToString(),
        c.WhenWoundState?.ToString(),
        c.NarrativeFragmentKey,
        c.RewardCursePoolKey,
        c.EncounterKey,
        c.RelationshipDelta,
        c.MemoryFlag,
        c.WoundKey,
        c.OfferingKey,
        c.OnWin?.Select(FromDomain).ToArray(),
        c.OnFlee?.Select(FromDomain).ToArray(),
        c.OnLose?.Select(FromDomain).ToArray());
}