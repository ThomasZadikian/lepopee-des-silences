using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

public sealed class GridRoomLayoutTemplateProvider : IGridRoomLayoutTemplateProvider
{
    private readonly IReadOnlyDictionary<string, GridRoomLayoutTemplate> _templatesByRoomType;
    private readonly IReadOnlyDictionary<string, GridRoomLayoutTemplate> _templatesByCatalogKey;

    public GridRoomLayoutTemplateProvider()
    {
        var tactical = DefaultGridRoomLayoutTemplates.DefaultTacticalV1;
        _templatesByRoomType = new Dictionary<string, GridRoomLayoutTemplate>
        {
            [KeyFor(tactical.RoomType, tactical.Version)] = tactical
        };

        // Catalog-room-specific templates take priority over the generic per-RoomType one — see
        // HardcodedRoomStructuralProfileProvider, which profiles the same five catalog rooms.
        _templatesByCatalogKey = new Dictionary<string, GridRoomLayoutTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            ["room.jardin"] = DefaultGridRoomLayoutTemplates.JardinV1,
            ["room.hopital"] = DefaultGridRoomLayoutTemplates.HopitalV1,
            ["room.enfer3"] = DefaultGridRoomLayoutTemplates.Enfer3V1,
            ["room.cavernedecrystal"] = DefaultGridRoomLayoutTemplates.CaverneDeCrystalV1,
            ["room.labyrinthe"] = DefaultGridRoomLayoutTemplates.LabyrintheV1,
            ["room.halldentree"] = DefaultGridRoomLayoutTemplates.HallDentreeV1,
        };
    }

    public GridRoomLayoutTemplate GetTemplate(RoomType roomType, string generatorVersion, string? catalogRoomKey = null)
    {
        if (catalogRoomKey is not null
            && generatorVersion == DefaultGridRoomLayoutTemplates.GeneratorVersion
            && _templatesByCatalogKey.TryGetValue(catalogRoomKey, out var catalogTemplate))
        {
            return catalogTemplate;
        }

        var key = KeyFor(roomType, generatorVersion);

        if (_templatesByRoomType.TryGetValue(key, out var template))
        {
            return template;
        }

        if (generatorVersion == DefaultGridRoomLayoutTemplates.GeneratorVersion)
        {
            return _templatesByRoomType.Values.First();
        }

        throw new KeyNotFoundException(
            $"No GridRoomLayoutTemplate found for RoomType '{roomType}' and version '{generatorVersion}'.");
    }

    private static string KeyFor(RoomType roomType, string version)
    {
        return $"{roomType}::{version}";
    }
}
