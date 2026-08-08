using Leds.GameEngine.Application.Catalog;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Reachability;

/// <summary>
/// Picks the next catalog room from the current room's explicit reachability graph
/// (SFD "Refonte des Rooms" § 5). Only applies to rooms that belong to a World — content
/// with no World assigned (e.g. the Pittsburgh / L'épopée des Échos canon rooms, out of
/// scope for the beta) is untouched and keeps using the legacy theme-based selection in
/// <see cref="Leds.GameEngine.Infrastructure.Generation.DeterministicRunGenerator"/>.
/// </summary>
public interface IRoomReachabilitySelector
{
    /// <summary>
    /// Returns the next room, or null when the current room isn't part of a reachability
    /// graph (no World assigned, or its World/entry room can't be resolved) — callers
    /// should fall back to the legacy theme-based selection in that case.
    /// </summary>
    CatalogRoomDefinition? SelectNextRoom(
        CatalogRoomDefinition currentRoom,
        IReadOnlyCollection<CatalogRoomDefinition> allRoomDefinitions,
        IReadOnlyCollection<CatalogWorldDefinition> worlds,
        IReadOnlyCollection<CatalogRoomThemeAffinity> themeAffinities,
        int nextRoomDepth,
        IReadOnlyCollection<string> visitedRoomKeysThisRun,
        string seed);

    /// <summary>
    /// Returns every room the current one can lead to at <paramref name="nextRoomDepth"/> —
    /// one exit per candidate, unweighted (the player chooses, so there is nothing left to
    /// roll for). Falls back to a single-element result (the World's entry room) when there
    /// are no eligible candidates, same "never a true dead end" guarantee as
    /// <see cref="SelectNextRoom"/>. Empty only when the current room isn't part of a
    /// reachability graph at all (no World assigned) — callers should fall back to the
    /// legacy theme-based selection in that case, same as <see cref="SelectNextRoom"/>.
    /// </summary>
    IReadOnlyCollection<CatalogRoomDefinition> SelectEligibleRooms(
        CatalogRoomDefinition currentRoom,
        IReadOnlyCollection<CatalogRoomDefinition> allRoomDefinitions,
        IReadOnlyCollection<CatalogWorldDefinition> worlds,
        int nextRoomDepth);
}
