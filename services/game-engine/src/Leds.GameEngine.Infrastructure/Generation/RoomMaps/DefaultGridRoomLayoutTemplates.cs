using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

public static class DefaultGridRoomLayoutTemplates
{
    public const string GeneratorVersion = "grid-room-layout-1.0.0";

    // BALANCE KNOB — a single v1 grid shape used for every RoomType, mirroring the Classic
    // scaffold's DefaultThresholdV1 (which is likewise reused for every RoomType today).
    //
    // Sized for the exploration-camera plan (Workstream B): 26x18 (~3.3x the previous 14x10) is
    // the recommended ceiling before the O(n^2) Dijkstra pathfinders (RoomGrid.FindPath server-
    // side, buildMovementRange client-side — both explicitly sized around "the board is ~80-140
    // cells") need an algorithmic rewrite. Node count and the movement-budget floor scale with
    // it; ObstacleDensity/FloorCarveDensity are already grid-size fractions and need no change.
    //
    // maxNodeCount is capped at 30, not scaled all the way up with the grid — Run.StartNew
    // hard-rejects an initial room outside [6, 30] nodes (this template is reused for every
    // RoomType, including the first), so going higher would make the very first room a run
    // sometimes fails to start with.
    public static readonly GridRoomLayoutTemplate DefaultTacticalV1 = new(
        key: "tactical-default-v1",
        version: GeneratorVersion,
        roomType: RoomType.Threshold,
        width: 26,
        height: 18,
        movementBudget: 78,
        minNodeCount: 22,
        maxNodeCount: 30,
        startX: 0,
        startY: 9);
}
