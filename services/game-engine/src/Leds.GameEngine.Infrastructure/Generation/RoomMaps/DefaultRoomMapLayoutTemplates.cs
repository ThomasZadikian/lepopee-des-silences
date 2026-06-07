using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

public static class DefaultRoomMapLayoutTemplates
{
    public const string GeneratorVersion = "room-map-layout-1.0.0";

    public static readonly RoomMapLayoutTemplate DefaultThresholdV1 = new(
        key: "threshold-default-v1",
        version: GeneratorVersion,
        roomType: RoomType.Threshold,
        rowNodeCounts: new[] { 2, 3, 4, 3, 4, 3, 2, 1 });
}
