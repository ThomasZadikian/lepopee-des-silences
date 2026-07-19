using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

public static class DefaultGridRoomLayoutTemplates
{
    public const string GeneratorVersion = "grid-room-layout-1.0.0";

    // BALANCE KNOB — a single v1 grid shape used for every RoomType, mirroring the Classic
    // scaffold's DefaultThresholdV1 (which is likewise reused for every RoomType today).
    public static readonly GridRoomLayoutTemplate DefaultTacticalV1 = new(
        key: "tactical-default-v1",
        version: GeneratorVersion,
        roomType: RoomType.Threshold,
        width: 10,
        height: 8,
        movementBudget: 26,
        minNodeCount: 10,
        maxNodeCount: 14,
        startX: 0,
        startY: 4);
}
