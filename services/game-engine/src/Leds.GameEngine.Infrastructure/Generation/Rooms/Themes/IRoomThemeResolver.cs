using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;

public interface IRoomThemeResolver
{
    string Resolve(RoomType roomType);
}