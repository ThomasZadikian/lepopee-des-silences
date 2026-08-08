using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Application.RoomMaps;

/// <summary>
/// How a floor-carving pass shapes a room's outer silhouette. <see cref="Rectangular"/> bites
/// blobs out of a bounding rectangle's edges (the only style the generator has today — alcoves,
/// L-shapes, ragged borders, and optionally interior sub-rooms partitioned off behind a door).
/// <see cref="Organic"/> carves a wavering radius around the room's center instead (jardin,
/// caverne, clairière-style rooms per the Claude Design handoff) and never produces sub-rooms.
/// </summary>
public enum CarvingStyle
{
    Rectangular = 0,
    Organic = 1,
}

/// <summary>
/// Defines the STRUCTURAL generation rules for a room — its silhouette and whether it may
/// contain built sub-rooms — as opposed to <see cref="RoomTypeGenerationProfile"/>, which only
/// governs node content (which event types appear, at what risk). Keyed by catalog room identity
/// (see <see cref="IRoomStructuralProfileProvider"/>), not by the coarse <c>RoomType</c> family,
/// because two rooms sharing a RoomType (e.g. both "Forest") can call for entirely different
/// silhouettes — a jardin's organic clearing has nothing in common with another Forest-themed
/// room built from rectangular chambers.
/// </summary>
public sealed record RoomStructuralProfile
{
    public RoomStructuralProfile(CarvingStyle carvingStyle, bool subRoomsAllowed)
    {
        if (carvingStyle == CarvingStyle.Organic && subRoomsAllowed)
        {
            throw new DomainException(
                "An Organic-carved room can never allow sub-rooms — its contour IS the room, by construction.");
        }

        CarvingStyle = carvingStyle;
        SubRoomsAllowed = subRoomsAllowed;
    }

    public CarvingStyle CarvingStyle { get; }

    public bool SubRoomsAllowed { get; }

    /// <summary>
    /// The generic fallback for any catalog room without a dedicated profile — today's only
    /// behavior (rectangular edge-carving, no sub-rooms), preserved exactly so unprofiled rooms
    /// are unaffected by this feature's introduction.
    /// </summary>
    public static RoomStructuralProfile Default { get; } = new(CarvingStyle.Rectangular, subRoomsAllowed: false);
}
