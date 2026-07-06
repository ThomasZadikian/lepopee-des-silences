using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Interlude.Dtos;

/// <summary>
/// Minimal run summary shown on the interlude screen.
/// Only includes data that is reliably available at the end of any room.
/// </summary>
public sealed record RunSummaryDto(
    string Seed,
    int CurrentRoomIndex,
    int DisplayRoomNumber,
    string CurrentRoomType,
    int ActivePalaceLawCount,
    IReadOnlyCollection<NpcEncounterSummaryDto> NpcEncounters)
{
    public static RunSummaryDto FromDomain(Run run) =>
        new(
            run.Seed,
            run.CurrentRoomIndex,
            run.CurrentRoomIndex + 1,
            run.CurrentRoom.RoomType.ToString(),
            run.ActivePalaceLaws.Count,
            run.NpcRelationships.Select(NpcEncounterSummaryDto.FromDomain).ToArray());
}

/// <summary>
/// Data foundation for the future end-of-run recap screen (SFD § 9.2) — no dedicated
/// screen yet, this only exposes what's already tracked on the Run aggregate.
/// </summary>
public sealed record NpcEncounterSummaryDto(
    string NpcKey,
    int RelationshipScore,
    string AggregateState,
    int TimesMet)
{
    public static NpcEncounterSummaryDto FromDomain(Domain.Npcs.NpcRelationship relationship) =>
        new(
            relationship.NpcKey,
            relationship.RelationshipScore,
            relationship.AggregateState.ToString(),
            relationship.TimesMet);
}