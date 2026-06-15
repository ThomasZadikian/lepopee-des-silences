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
    int ActivePalaceLawCount)
{
    public static RunSummaryDto FromDomain(Run run) =>
        new(
            run.Seed,
            run.CurrentRoomIndex,
            run.CurrentRoomIndex + 1,
            run.CurrentRoom.RoomType.ToString(),
            run.ActivePalaceLaws.Count);
}