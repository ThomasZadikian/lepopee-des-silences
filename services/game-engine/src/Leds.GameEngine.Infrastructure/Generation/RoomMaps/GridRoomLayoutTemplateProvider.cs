using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

public sealed class GridRoomLayoutTemplateProvider : IGridRoomLayoutTemplateProvider
{
    private readonly IReadOnlyDictionary<string, GridRoomLayoutTemplate> _templates;

    public GridRoomLayoutTemplateProvider()
    {
        var tactical = DefaultGridRoomLayoutTemplates.DefaultTacticalV1;
        _templates = new Dictionary<string, GridRoomLayoutTemplate>
        {
            [KeyFor(tactical.RoomType, tactical.Version)] = tactical
        };
    }

    public GridRoomLayoutTemplate GetTemplate(RoomType roomType, string generatorVersion)
    {
        var key = KeyFor(roomType, generatorVersion);

        if (_templates.TryGetValue(key, out var template))
        {
            return template;
        }

        if (generatorVersion == DefaultGridRoomLayoutTemplates.GeneratorVersion)
        {
            return _templates.Values.First();
        }

        throw new KeyNotFoundException(
            $"No GridRoomLayoutTemplate found for RoomType '{roomType}' and version '{generatorVersion}'.");
    }

    private static string KeyFor(RoomType roomType, string version)
    {
        return $"{roomType}::{version}";
    }
}
