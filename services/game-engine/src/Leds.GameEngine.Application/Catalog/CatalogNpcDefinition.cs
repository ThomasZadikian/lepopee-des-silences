using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogNpcDefinition(
    string Key,
    string DisplayName,
    string Description,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> CompatibleRoomTypes,
    IReadOnlyCollection<PalaceRoomState> CompatiblePalaceRoomStates,
    IReadOnlyCollection<string> CompatibleRoomClimates,
    int MinDepth = 0,
    int MaxDepth = int.MaxValue);
