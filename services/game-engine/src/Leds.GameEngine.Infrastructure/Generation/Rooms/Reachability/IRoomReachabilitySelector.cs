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
}
