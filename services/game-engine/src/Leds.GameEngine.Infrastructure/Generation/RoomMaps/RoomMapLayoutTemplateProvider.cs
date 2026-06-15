using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

public sealed class RoomMapLayoutTemplateProvider : IRoomMapLayoutTemplateProvider
{
    private readonly IReadOnlyDictionary<string, RoomMapLayoutTemplate> _templates;

    public RoomMapLayoutTemplateProvider()
    {
        var threshold = DefaultRoomMapLayoutTemplates.DefaultThresholdV1;
        _templates = new Dictionary<string, RoomMapLayoutTemplate>
        {
            [KeyFor(threshold.RoomType, threshold.Version)] = threshold
        };
    }

    public RoomMapLayoutTemplate GetTemplate(RoomType roomType, string generatorVersion)
    {
        var key = KeyFor(roomType, generatorVersion);

        if (_templates.TryGetValue(key, out var template))
        {
            return template;
        }

        if (generatorVersion == DefaultRoomMapLayoutTemplates.GeneratorVersion)
        {
            return _templates.Values.First();
        }

        throw new KeyNotFoundException(
            $"No RoomMapLayoutTemplate found for RoomType '{roomType}' and version '{generatorVersion}'.");
    }

    private static string KeyFor(RoomType roomType, string version)
    {
        return $"{roomType}::{version}";
    }
}