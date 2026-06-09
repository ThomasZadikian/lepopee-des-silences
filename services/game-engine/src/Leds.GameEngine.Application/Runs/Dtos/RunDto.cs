using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.Dtos;

/// <param name="CurrentRoomIndex">
/// Zero-based position of the current room in the infinite run sequence.
/// Threshold room = 0. Incremented by MoveToNextRoom (future).
/// </param>
/// <param name="CurrentRoomNumber">
/// One-based room number for player display ("Salle 1", "Salle 2", …).
/// Always equals <c>CurrentRoomIndex + 1</c>. Display-only — do not use for game logic.
/// </param>
/// <param name="CanResume">
/// <c>true</c> when the run is in <see cref="RunStatus.Suspended"/> state and can be resumed.
/// </param>
/// <param name="SavedAt">
/// The timestamp at which the player saved and exited the run. <c>null</c> if never suspended.
/// </param>
/// <param name="AbandonedAt">
/// The timestamp at which the run was abandoned (<see cref="RunStatus.Abandoned"/>).
/// Equals <c>EndedAt</c> on the domain object. <c>null</c> for non-abandoned runs.
/// </param>
public sealed record RunDto(
    Guid Id,
    Guid PlayerId,
    string Seed,
    string GeneratorVersion,
    string MarkovMatrixVersion,
    string Status,
    int CurrentDepth,
    Guid? ActiveCombatId,
    Guid? PendingRewardOfferId,
    RoomDto CurrentRoom,
    IReadOnlyCollection<RoomDto> Rooms,
    IReadOnlyCollection<ActivePalaceLawDto> ActivePalaceLaws,
    int CurrentRoomIndex,
    int CurrentRoomNumber,
    bool CanResume,
    DateTimeOffset? SavedAt,
    DateTimeOffset? AbandonedAt)
{
    public static RunDto FromDomain(Run run)
    {
        return new RunDto(
            run.Id.Value,
            run.PlayerId,
            run.Seed,
            run.GeneratorVersion,
            run.MarkovMatrixVersion,
            run.Status.ToString(),
            run.CurrentDepth,
            run.ActiveCombatId?.Value,
            run.PendingRewardOfferId?.Value,
            RoomDto.FromDomain(run.CurrentRoom),
            run.Rooms.Select(RoomDto.FromDomain).ToArray(),
            run.ActivePalaceLaws.Select(ActivePalaceLawDto.FromDomain).ToArray(),
            run.CurrentRoomIndex,
            run.CurrentRoomIndex + 1,
            CanResume: run.Status == RunStatus.Suspended,
            SavedAt: run.SavedAt,
            AbandonedAt: run.Status == RunStatus.Abandoned ? run.EndedAt : null);
    }
}