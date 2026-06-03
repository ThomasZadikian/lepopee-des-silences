using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

public interface IRoomTypeResolver
{
    RoomType ResolveNextRoomType(
        string seed,
        int nextRoomDepth,
        RoomType currentRoomType,
        string matrixVersion);
}