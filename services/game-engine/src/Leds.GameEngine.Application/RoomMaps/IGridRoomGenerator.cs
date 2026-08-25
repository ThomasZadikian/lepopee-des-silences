using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.RoomMaps;

/// <summary>
/// Tactical-mode counterpart of <see cref="IMapRoomGenerator"/> — produces a
/// <see cref="Room"/> built via <see cref="Room.CreateGrid"/> (free-movement grid) instead of
/// the Classic row/lane DAG.
/// </summary>
public interface IGridRoomGenerator
{
    /// <summary>
    /// <paramref name="catalogRoomKey"/>, when the caller already knows the specific catalog
    /// room this shape is being generated for (e.g. "room.jardin"), lets the generator pick a
    /// room-specific layout template and structural profile (silhouette carving style, whether
    /// sub-rooms are allowed) instead of falling back to the generic per-RoomType defaults.
    /// </summary>
    Task<Room> GenerateAsync(
        string seed,
        string generatorVersion,
        int roomDepth,
        RoomType roomType,
        Random random,
        CancellationToken cancellationToken = default,
        PalaceRoomState palaceState = PalaceRoomState.Neutral,
        string? catalogRoomKey = null,
        string? bossDefinitionKey = null);
}
