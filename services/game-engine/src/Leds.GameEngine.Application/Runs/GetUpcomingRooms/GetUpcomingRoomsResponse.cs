namespace Leds.GameEngine.Application.Runs.GetUpcomingRooms;

/// <param name="IsRevealed">False when "Édit des Portes Ouvertes" (law.portes-ouvertes) is
/// not currently active — <see cref="Rooms"/> is then always empty.</param>
public sealed record GetUpcomingRoomsResponse(
    Guid RunId,
    bool IsRevealed,
    IReadOnlyCollection<UpcomingRoomDto> Rooms);

/// <param name="DisplayName">Null when that room slot has no matching catalog room and
/// stays purely procedural — nothing to reveal for it.</param>
public sealed record UpcomingRoomDto(
    int RoomIndex,
    string? Key,
    string? DisplayName);
