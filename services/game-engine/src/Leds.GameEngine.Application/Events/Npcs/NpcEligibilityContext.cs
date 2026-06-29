using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Events.Npcs;

public sealed record NpcEligibilityContext(
    Guid RunId,
    Guid RoomId,
    Guid NodeId,
    string Seed,
    PalaceRoomState PalaceRoomState,
    string? RoomClimate,
    RoomType RoomType,
    int NodeDepth,
    int RoomDepth = 0);
