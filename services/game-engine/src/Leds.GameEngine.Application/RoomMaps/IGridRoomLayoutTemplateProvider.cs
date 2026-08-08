using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.RoomMaps;

public interface IGridRoomLayoutTemplateProvider
{
    /// <summary>
    /// <paramref name="catalogRoomKey"/>, when known, is tried first for a room-specific
    /// template (e.g. "room.jardin"'s own dimensions) before falling back to the generic
    /// per-RoomType template.
    /// </summary>
    GridRoomLayoutTemplate GetTemplate(RoomType roomType, string generatorVersion, string? catalogRoomKey = null);
}
