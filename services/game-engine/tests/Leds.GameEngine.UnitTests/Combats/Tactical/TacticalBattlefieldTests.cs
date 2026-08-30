using FluentAssertions;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalBattlefieldTests
{
    private static RoomGrid CreateGrid(
        int width, int height, int startX, int startY,
        (int X, int Y)? obstacle = null, (int X, int Y, int Level)? elevated = null)
    {
        var elevation = new int[width * height];
        if (elevated is { } e)
        {
            elevation[(e.Y * width) + e.X] = e.Level;
        }

        var obstacles = obstacle is { } o
            ? new[] { o }
            : Array.Empty<(int X, int Y)>();

        return RoomGrid.CreateInitial(
            width, height, movementBudget: 100,
            startX: startX, startY: startY, nodes: [],
            elevation: elevation, obstacles: obstacles);
    }

    [Fact]
    public void FromRoomGridRegion_ShouldOffsetCellsByOrigin_WhenRegionIsFullyInsideTheGrid()
    {
        var grid = CreateGrid(
            width: 10, height: 10, startX: 0, startY: 0,
            obstacle: (6, 6), elevated: (5, 5, 3));

        var battlefield = TacticalBattlefield.FromRoomGridRegion(grid, originX: 4, originY: 4, width: 4, height: 4);

        battlefield.Width.Should().Be(4);
        battlefield.Height.Should().Be(4);
        battlefield.OriginX.Should().Be(4);
        battlefield.OriginY.Should().Be(4);

        // Grid (6,6) -> local (2,2); grid (5,5) -> local (1,1).
        battlefield.IsObstacle(new GridPosition(2, 2)).Should().BeTrue();
        battlefield.ElevationAt(new GridPosition(1, 1)).Should().Be(3);
        battlefield.IsObstacle(new GridPosition(1, 1)).Should().BeFalse();
    }

    [Fact]
    public void FromRoomGridRegion_ShouldClampOrigin_WhenRequestedRegionOverflowsTheBottomRightEdge()
    {
        var grid = CreateGrid(width: 10, height: 10, startX: 0, startY: 0);

        var battlefield = TacticalBattlefield.FromRoomGridRegion(grid, originX: 8, originY: 8, width: 5, height: 5);

        battlefield.Width.Should().Be(5);
        battlefield.Height.Should().Be(5);
        battlefield.OriginX.Should().Be(5);
        battlefield.OriginY.Should().Be(5);
    }

    [Fact]
    public void FromRoomGridRegion_ShouldClampOrigin_WhenRequestedRegionOverflowsTheTopLeftEdge()
    {
        var grid = CreateGrid(width: 10, height: 10, startX: 0, startY: 0);

        var battlefield = TacticalBattlefield.FromRoomGridRegion(grid, originX: -3, originY: -3, width: 5, height: 5);

        battlefield.OriginX.Should().Be(0);
        battlefield.OriginY.Should().Be(0);
        battlefield.Width.Should().Be(5);
        battlefield.Height.Should().Be(5);
    }

    [Fact]
    public void FromRoomGridRegion_ShouldReturnTheWholeRoom_WhenTheRoomIsSmallerThanTheRequestedRegion()
    {
        var grid = CreateGrid(width: 6, height: 6, startX: 0, startY: 0);

        var battlefield = TacticalBattlefield.FromRoomGridRegion(grid, originX: 10, originY: 10, width: 21, height: 21);

        battlefield.Width.Should().Be(6);
        battlefield.Height.Should().Be(6);
        battlefield.OriginX.Should().Be(0);
        battlefield.OriginY.Should().Be(0);
    }

    [Fact]
    public void FromRoomGridRegion_ShouldThrow_WhenWidthOrHeightIsNotPositive()
    {
        var grid = CreateGrid(width: 6, height: 6, startX: 0, startY: 0);

        var act = () => TacticalBattlefield.FromRoomGridRegion(grid, originX: 0, originY: 0, width: 0, height: 5);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void FromRoomGrid_ShouldStillProduceOriginZeroZero_ForBackwardCompatibility()
    {
        var grid = CreateGrid(width: 12, height: 9, startX: 0, startY: 0);

        var battlefield = TacticalBattlefield.FromRoomGrid(grid);

        battlefield.Width.Should().Be(12);
        battlefield.Height.Should().Be(9);
        battlefield.OriginX.Should().Be(0);
        battlefield.OriginY.Should().Be(0);
    }

    [Fact]
    public void Rehydrate_ShouldDefaultOriginToZeroZero_WhenNotProvided()
    {
        var battlefield = TacticalBattlefield.Rehydrate(
            width: 3, height: 3,
            elevation: new int[9], walkable: Enumerable.Repeat(true, 9).ToArray());

        battlefield.OriginX.Should().Be(0);
        battlefield.OriginY.Should().Be(0);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreTheProvidedOrigin()
    {
        var battlefield = TacticalBattlefield.Rehydrate(
            width: 3, height: 3,
            elevation: new int[9], walkable: Enumerable.Repeat(true, 9).ToArray(),
            isFloor: null, originX: 7, originY: 11);

        battlefield.OriginX.Should().Be(7);
        battlefield.OriginY.Should().Be(11);
    }
}
