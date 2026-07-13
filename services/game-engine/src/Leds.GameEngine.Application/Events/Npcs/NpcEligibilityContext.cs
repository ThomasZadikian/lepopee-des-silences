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
    int RoomDepth = 0,
    string? RoomKey = null,
    // NPC keys of already-recruited companions — never re-selected for an encounter.
    IReadOnlyCollection<string>? RecruitedCompanionNpcKeys = null);
