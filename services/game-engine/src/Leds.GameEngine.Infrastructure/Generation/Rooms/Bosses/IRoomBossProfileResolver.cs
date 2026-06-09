using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;

public interface IRoomBossProfileResolver
{
    Task<RoomBossProfile> ResolveAsync(RoomType roomType, CancellationToken cancellationToken = default);
}