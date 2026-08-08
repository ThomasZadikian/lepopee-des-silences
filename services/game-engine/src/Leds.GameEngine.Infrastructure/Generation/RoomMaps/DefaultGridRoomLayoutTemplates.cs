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

    // BALANCE KNOB — dedicated grids for the five typologies the Claude Design "salle" handoff
    // profiled precisely (see HardcodedRoomStructuralProfileProvider). Dimensions match the
    // handoff's own reference sizes verbatim (all comfortably under the 35x25 ceiling agreed for
    // this integration) — movementBudget/node counts scaled proportionally from
    // DefaultTacticalV1's 26x18 baseline (78 budget / 22-30 nodes). Keyed by catalog room Key,
    // NOT RoomType, in GridRoomLayoutTemplateProvider — several catalog rooms can share the same
    // coarse RoomType family while needing entirely different dimensions/budgets.

    public static readonly GridRoomLayoutTemplate JardinV1 = new(
        key: "room.jardin-v1",
        version: GeneratorVersion,
        roomType: RoomType.Forest,
        width: 26,
        height: 18,
        movementBudget: 78,
        minNodeCount: 22,
        maxNodeCount: 30,
        startX: 0,
        startY: 9);

    public static readonly GridRoomLayoutTemplate HopitalV1 = new(
        key: "room.hopital-v1",
        version: GeneratorVersion,
        roomType: RoomType.Silence,
        width: 26,
        height: 16,
        movementBudget: 70,
        minNodeCount: 20,
        maxNodeCount: 27,
        startX: 0,
        startY: 8);

    public static readonly GridRoomLayoutTemplate Enfer3V1 = new(
        key: "room.enfer3-v1",
        version: GeneratorVersion,
        roomType: RoomType.Rupture,
        width: 24,
        height: 18,
        movementBudget: 72,
        minNodeCount: 20,
        maxNodeCount: 28,
        startX: 0,
        startY: 9);

    public static readonly GridRoomLayoutTemplate CaverneDeCrystalV1 = new(
        key: "room.cavernedecrystal-v1",
        version: GeneratorVersion,
        roomType: RoomType.Threshold,
        width: 24,
        height: 18,
        movementBudget: 72,
        minNodeCount: 20,
        maxNodeCount: 28,
        startX: 0,
        startY: 9);

    public static readonly GridRoomLayoutTemplate LabyrintheV1 = new(
        key: "room.labyrinthe-v1",
        version: GeneratorVersion,
        roomType: RoomType.Rupture,
        width: 26,
        height: 18,
        movementBudget: 78,
        minNodeCount: 22,
        maxNodeCount: 30,
        startX: 0,
        startY: 9);
}
