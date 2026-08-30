using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

/// <summary>
/// Statically-defined structural profiles, keyed by catalog room <c>Key</c> — deliberately a
/// local lookup inside game-engine (mirroring <see cref="HardcodedRoomTypeGenerationProfileProvider"/>'s
/// own pattern) rather than a new field on the remote Catalog service's contract, since only
/// generation cares about a room's silhouette. Only the five typologies the Claude Design handoff
/// profiled precisely (Jardin/Hôpital/Forge/Caverne/Labyrinthe) have a dedicated entry; every
/// other catalog room falls back to <see cref="RoomStructuralProfile.Default"/> — today's only
/// behavior — until it's profiled individually.
/// </summary>
public sealed class HardcodedRoomStructuralProfileProvider : IRoomStructuralProfileProvider
{
    private static readonly IReadOnlyDictionary<string, RoomStructuralProfile> ProfilesByCatalogKey =
        new Dictionary<string, RoomStructuralProfile>(StringComparer.OrdinalIgnoreCase)
        {
            // Jardin — organic clearing, no built enceinte (vegetation/terrain carves the shape).
            ["room.jardin"] = new RoomStructuralProfile(CarvingStyle.Organic, subRoomsAllowed: false),

            // Hôpital — corridor + chambers behind doors: the bâti serves the usage.
            ["room.hopital"] = new RoomStructuralProfile(CarvingStyle.Rectangular, subRoomsAllowed: true),

            // Forge (les enfers) — open pit + enclosed work platforms/stores.
            ["room.enfer3"] = new RoomStructuralProfile(CarvingStyle.Rectangular, subRoomsAllowed: true),

            // Caverne de crystal — organic rock contour, alcoves carved by terrain, not walls.
            ["room.cavernedecrystal"] = new RoomStructuralProfile(CarvingStyle.Organic, subRoomsAllowed: false),

            // Labyrinthe — solid partitions pierced by narrow openings, a blind cell at center.
            ["room.labyrinthe"] = new RoomStructuralProfile(CarvingStyle.Rectangular, subRoomsAllowed: true),
        };

    public RoomStructuralProfile GetProfile(RoomType roomType, string? catalogRoomKey)
    {
        if (catalogRoomKey is not null
            && ProfilesByCatalogKey.TryGetValue(catalogRoomKey, out var profile))
        {
            return profile;
        }

        return RoomStructuralProfile.Default;
    }
}
