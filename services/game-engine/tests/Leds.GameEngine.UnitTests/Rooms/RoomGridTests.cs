using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.UnitTests.Rooms;

public sealed class RoomGridTests
{
    private static MapNode CreateNode(int lane, int row) => MapNode.Create(
        NodeEventType.Item, riskLevel: 10, rewardProfile: "standard",
        row, lane, parentNodeIds: Array.Empty<NodeId>(),
        isBoss: false, initialState: NodeState.Available);

    private static int[] FlatElevation(int width, int height) => new int[width * height];

    private static int[] ElevationWith(int width, int height, params (int X, int Y, int Level)[] overrides)
    {
        var elevation = new int[width * height];

        foreach (var (x, y, level) in overrides)
        {
            elevation[(y * width) + x] = level;
        }

        return elevation;
    }

    [Fact]
    public void CreateInitial_ShouldPlacePartyAtStart_AndFillMovementBudget()
    {
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 4, nodes: []);

        grid.PartyX.Should().Be(0);
        grid.PartyY.Should().Be(4);
        grid.MovementBudget.Should().Be(20);
        grid.MovementBudgetRemaining.Should().Be(20);
        grid.StartX.Should().Be(0);
        grid.StartY.Should().Be(4);
    }

    [Fact]
    public void CreateInitial_ShouldDefaultToFlatElevation_AndNoObstacles()
    {
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 4, nodes: []);

        grid.Elevation.Should().AllSatisfy(level => level.Should().Be(0));
        grid.Obstacles.Should().BeEmpty();
    }

    [Fact]
    public void CreateInitial_ShouldRevealCellsWithinVisionRadiusOfStart()
    {
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 5, startY: 4, nodes: []);

        grid.RevealedCells.Should().Contain((5, 4));
        grid.RevealedCells.Should().Contain((5 + RoomGrid.VisionRadius, 4));
        grid.RevealedCells.Should().NotContain((5 + RoomGrid.VisionRadius + 1, 4));
    }

    [Fact]
    public void CreateInitial_ShouldRevealNodesWithinVisionRadius()
    {
        var closeNode = CreateNode(lane: 1, row: 4);
        var farNode = CreateNode(lane: 9, row: 4);

        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 4, [closeNode, farNode]);

        grid.RevealedNodeIds.Should().Contain(closeNode.Id);
        grid.RevealedNodeIds.Should().NotContain(farNode.Id);
    }

    [Theory]
    [InlineData(-1, 4, 0)]
    [InlineData(10, 4, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 8, 0)]
    public void CreateInitial_ShouldRejectInvalidDimensionsOrStart(int startX, int startY, int unused)
    {
        _ = unused;
        var act = () => RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX, startY, nodes: []);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateInitial_ShouldRejectNegativeMovementBudget()
    {
        var act = () => RoomGrid.CreateInitial(10, 8, movementBudget: -1, startX: 0, startY: 0, nodes: []);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateInitial_ShouldRejectElevationOfWrongLength()
    {
        var act = () => RoomGrid.CreateInitial(
            10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: [],
            elevation: new int[5]);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(RoomGrid.MaxElevation + 1)]
    public void CreateInitial_ShouldRejectElevationOutOfRange(int invalidLevel)
    {
        var elevation = FlatElevation(10, 8);
        elevation[0] = invalidLevel;

        var act = () => RoomGrid.CreateInitial(
            10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: [], elevation: elevation);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateInitial_ShouldRejectObstacleOutOfBounds()
    {
        var act = () => RoomGrid.CreateInitial(
            10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: [],
            obstacles: [(10, 0)]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateInitial_ShouldRejectObstacleOnStartCell()
    {
        var act = () => RoomGrid.CreateInitial(
            10, 8, movementBudget: 20, startX: 3, startY: 3, nodes: [],
            obstacles: [(3, 3)]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveTo_ShouldUpdatePartyPosition_AndDeductCost()
    {
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 4, nodes: []);

        grid.MoveTo([(1, 4), (2, 4), (3, 4)], cost: 3, nodes: []);

        grid.PartyX.Should().Be(3);
        grid.PartyY.Should().Be(4);
        grid.MovementBudgetRemaining.Should().Be(17);
    }

    [Fact]
    public void MoveTo_ShouldRevealCellsAlongThePath_NotJustTheDestination()
    {
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: []);

        grid.MoveTo([(1, 0), (2, 0), (3, 0), (4, 0), (5, 0), (6, 0)], cost: 6, nodes: []);

        grid.RevealedCells.Should().Contain((3, 0),
            "fog of war should reveal cells passed through, not only the final destination.");
    }

    [Fact]
    public void MoveTo_ShouldRevealNodesNewlyInRange()
    {
        var farNode = CreateNode(lane: 6, row: 0);
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 0, [farNode]);

        grid.RevealedNodeIds.Should().NotContain(farNode.Id);

        grid.MoveTo([(1, 0), (2, 0), (3, 0), (4, 0), (5, 0), (6, 0)], cost: 6, [farNode]);

        grid.RevealedNodeIds.Should().Contain(farNode.Id);
    }

    [Fact]
    public void ResetToInitial_ShouldRestorePositionBudgetAndClearFog()
    {
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 4, nodes: []);
        grid.MoveTo([(1, 4), (2, 4), (3, 4), (4, 4), (5, 4)], cost: 5, nodes: []);

        grid.ResetToInitial(nodes: []);

        grid.PartyX.Should().Be(0);
        grid.PartyY.Should().Be(4);
        grid.MovementBudgetRemaining.Should().Be(20);
        grid.RevealedCells.Should().NotContain((5, 4));
    }

    [Fact]
    public void ResetToInitial_ShouldNotRegenerateElevationOrObstacles()
    {
        var elevation = ElevationWith(10, 8, (5, 4, 2));
        var grid = RoomGrid.CreateInitial(
            10, 8, movementBudget: 20, startX: 0, startY: 4, nodes: [],
            elevation: elevation, obstacles: [(7, 4)]);

        grid.ResetToInitial(nodes: []);

        grid.ElevationAt(5, 4).Should().Be(2);
        grid.IsObstacle(7, 4).Should().BeTrue();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreExactState()
    {
        var nodeId = NodeId.New();

        var grid = RoomGrid.Rehydrate(
            width: 10, height: 8, movementBudget: 20, movementBudgetRemaining: 12,
            startX: 0, startY: 4, partyX: 3, partyY: 4,
            revealedNodeIds: [nodeId], revealedCells: [(3, 4), (2, 4)],
            elevation: FlatElevation(10, 8), obstacles: []);

        grid.MovementBudgetRemaining.Should().Be(12);
        grid.PartyX.Should().Be(3);
        grid.PartyY.Should().Be(4);
        grid.RevealedNodeIds.Should().ContainSingle().Which.Should().Be(nodeId);
        grid.RevealedCells.Should().BeEquivalentTo(new[] { (3, 4), (2, 4) });
    }

    [Fact]
    public void Rehydrate_ShouldRestoreElevationAndObstacles()
    {
        var elevation = ElevationWith(10, 8, (1, 1, 3));

        var grid = RoomGrid.Rehydrate(
            width: 10, height: 8, movementBudget: 20, movementBudgetRemaining: 20,
            startX: 0, startY: 0, partyX: 0, partyY: 0,
            revealedNodeIds: [], revealedCells: [],
            elevation: elevation, obstacles: [(2, 2)]);

        grid.ElevationAt(1, 1).Should().Be(3);
        grid.IsObstacle(2, 2).Should().BeTrue();
        grid.IsObstacle(0, 0).Should().BeFalse();
    }

    [Fact]
    public void FindPath_ShouldReturnManhattanCostAndStraightPath_OnFlatOpenGrid()
    {
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: []);

        var route = grid.FindPath(3, 0);

        route.Should().NotBeNull();
        route!.Value.Cost.Should().Be(3);
        route.Value.Path.Should().BeEquivalentTo(
            new[] { (1, 0), (2, 0), (3, 0) }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void FindPath_ShouldReturnNull_WhenTargetIsAnObstacle()
    {
        var grid = RoomGrid.CreateInitial(
            10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: [], obstacles: [(3, 0)]);

        grid.FindPath(3, 0).Should().BeNull();
    }

    [Fact]
    public void FindPath_ShouldReturnNull_WhenTargetIsSealedOffByObstacles()
    {
        // Wall off (5,5) on all four orthogonal sides.
        var grid = RoomGrid.CreateInitial(
            10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: [],
            obstacles: [(4, 5), (6, 5), (5, 4), (5, 6)]);

        grid.FindPath(5, 5).Should().BeNull();
    }

    [Fact]
    public void FindPath_ShouldRouteAroundObstacle_AndPriceTheDetour()
    {
        // A single-row wall from (2,0) to (2,3) with a gap at (2,4) forces a detour.
        var grid = RoomGrid.CreateInitial(
            10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: [],
            obstacles: [(2, 0), (2, 1), (2, 2), (2, 3)]);

        var direct = Math.Abs(4 - 0) + Math.Abs(0 - 0);
        var route = grid.FindPath(4, 0);

        route.Should().NotBeNull();
        route!.Value.Cost.Should().BeGreaterThan(direct);
        route.Value.Path.Should().NotContain((2, 0));
        route.Value.Path.Should().NotContain((2, 1));
        route.Value.Path.Should().NotContain((2, 2));
        route.Value.Path.Should().NotContain((2, 3));
    }

    [Fact]
    public void FindPath_ShouldPriceElevationClimb_HigherThanAFlatStep()
    {
        var flatGrid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: []);
        var flatRoute = flatGrid.FindPath(1, 0);

        var slopedElevation = ElevationWith(10, 8, (1, 0, 2));
        var slopedGrid = RoomGrid.CreateInitial(
            10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: [], elevation: slopedElevation);
        var slopedRoute = slopedGrid.FindPath(1, 0);

        flatRoute!.Value.Cost.Should().Be(1);
        slopedRoute!.Value.Cost.Should().Be(3, "climbing 2 elevation levels should cost 1 + 2 = 3.");
    }

    [Fact]
    public void FindPath_DescendingElevation_ShouldCostTheSameAsAFlatStep()
    {
        var elevation = ElevationWith(10, 8, (0, 0, 3));
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: [], elevation: elevation);

        var route = grid.FindPath(1, 0);

        route!.Value.Cost.Should().Be(1, "descending should never cost extra.");
    }

    [Fact]
    public void RevealAround_ShouldRevealFewerCells_BehindATallRidge_ThanOnAnEquivalentFlatBoard()
    {
        // Viewer at (2,2), candidate target 2 cells north at (2,0), single intermediate cell at
        // (2,1) directly on the line between them.
        var ridgeElevation = ElevationWith(5, 5, (2, 1, RoomGrid.MaxElevation));
        var ridgeGrid = RoomGrid.CreateInitial(5, 5, movementBudget: 20, startX: 2, startY: 2, nodes: [], elevation: ridgeElevation);

        var flatGrid = RoomGrid.CreateInitial(5, 5, movementBudget: 20, startX: 2, startY: 2, nodes: []);

        ridgeGrid.RevealedCells.Should().NotContain((2, 0),
            "a height-3 ridge directly between viewer and target should block the sightline.");
        flatGrid.RevealedCells.Should().Contain((2, 0),
            "the same cell should be visible with no ridge in the way.");
    }

    [Fact]
    public void RevealAround_ShouldAlwaysBlockThroughAnObstacle_RegardlessOfHeight()
    {
        var grid = RoomGrid.CreateInitial(
            5, 5, movementBudget: 20, startX: 2, startY: 2, nodes: [], obstacles: [(2, 1)]);

        grid.RevealedCells.Should().NotContain((2, 0),
            "an obstacle on the sightline blocks vision even when elevation alone would not.");
    }

    [Fact]
    public void RevealAround_ShouldGateNodeVisibility_BehindATallRidge_JustLikeCells()
    {
        var ridgeElevation = ElevationWith(5, 5, (2, 1, RoomGrid.MaxElevation));
        var farNode = CreateNode(lane: 2, row: 0);

        var grid = RoomGrid.CreateInitial(
            5, 5, movementBudget: 20, startX: 2, startY: 2, [farNode], elevation: ridgeElevation);

        grid.RevealedNodeIds.Should().NotContain(farNode.Id);
    }
}
