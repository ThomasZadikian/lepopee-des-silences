using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record EventContentResolutionContext(
    string Seed,
    RoomType RoomType,
    int RoomDepth,
    int NodeDepth,
    int EventOrder,
    NodeEventType EventType,
    int RiskLevel,
    string RewardProfile,
    IReadOnlyCollection<string>? ActivePalaceLawKeys = null,
    PalaceRoomState? PalaceRoomState = null,
    string? RoomClimate = null,
    string? RoomKey = null,
    // NPC keys of already-recruited companions (derived from the player's roster) — an
    // NPC in this set must never be re-selected for an encounter (see NpcEncounterSelector).
    IReadOnlyCollection<string>? RecruitedCompanionNpcKeys = null);
