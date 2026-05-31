using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;

public interface IRoomBossProfileResolver
{
    RoomBossProfile Resolve(RoomType roomType);
}