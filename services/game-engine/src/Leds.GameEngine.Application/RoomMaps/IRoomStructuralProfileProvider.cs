using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.RoomMaps;

/// <summary>
/// Resolves a <see cref="RoomStructuralProfile"/> for a room. <paramref name="catalogRoomKey"/>
/// is preferred when known (the specific named room, e.g. "room.jardin") — <paramref
/// name="roomType"/> is only a fallback for content with no catalog identity resolved yet (see
/// DeterministicRunGenerator's legacy no-WorldKey path).
/// </summary>
public interface IRoomStructuralProfileProvider
{
    RoomStructuralProfile GetProfile(RoomType roomType, string? catalogRoomKey);
}
