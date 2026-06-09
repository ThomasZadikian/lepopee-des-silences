using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.RoomMaps;

public interface IMapRoomGenerator
{
    Task<Room> GenerateAsync(
        string seed,
        string generatorVersion,
        int roomDepth,
        RoomType roomType,
        Random random,
        CancellationToken cancellationToken = default);
}
