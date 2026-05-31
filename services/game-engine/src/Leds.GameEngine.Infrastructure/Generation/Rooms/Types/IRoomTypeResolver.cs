using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

public interface IRoomTypeResolver
{
    RoomType Resolve(int roomDepth, Random random);
}