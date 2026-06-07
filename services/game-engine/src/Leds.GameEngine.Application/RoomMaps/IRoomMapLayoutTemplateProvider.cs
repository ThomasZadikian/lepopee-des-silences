using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.RoomMaps;

public interface IRoomMapLayoutTemplateProvider
{
    RoomMapLayoutTemplate GetTemplate(RoomType roomType, string generatorVersion);
}
