using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.RoomMaps;

/// <summary>
/// Tactical-mode counterpart of <see cref="IMapRoomGenerator"/> — produces a
/// <see cref="Room"/> built via <see cref="Room.CreateGrid"/> (free-movement grid) instead of
/// the Classic row/lane DAG.
/// </summary>
public interface IGridRoomGenerator
{
    Task<Room> GenerateAsync(
        string seed,
        string generatorVersion,
        int roomDepth,
        RoomType roomType,
        Random random,
        CancellationToken cancellationToken = default,
        PalaceRoomState palaceState = PalaceRoomState.Neutral);
}
