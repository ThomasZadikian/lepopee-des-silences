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
    [InlineData(0, 0, 0)]
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
    public void MoveTo_ShouldUpdatePartyPosition_AndDeductCost()
    {
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 4, nodes: []);

        grid.MoveTo(3, 4, cost: 3, nodes: []);

        grid.PartyX.Should().Be(3);
        grid.PartyY.Should().Be(4);
        grid.MovementBudgetRemaining.Should().Be(17);
    }

    [Fact]
    public void MoveTo_ShouldRevealCellsAlongThePath_NotJustTheDestination()
    {
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 0, nodes: []);

        grid.MoveTo(6, 0, cost: 6, nodes: []);

        grid.RevealedCells.Should().Contain((3, 0),
            "fog of war should reveal cells passed through, not only the final destination.");
    }

    [Fact]
    public void MoveTo_ShouldRevealNodesNewlyInRange()
    {
        var farNode = CreateNode(lane: 6, row: 0);
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 0, [farNode]);

        grid.RevealedNodeIds.Should().NotContain(farNode.Id);

        grid.MoveTo(6, 0, cost: 6, [farNode]);

        grid.RevealedNodeIds.Should().Contain(farNode.Id);
    }

    [Fact]
    public void ResetToInitial_ShouldRestorePositionBudgetAndClearFog()
    {
        var grid = RoomGrid.CreateInitial(10, 8, movementBudget: 20, startX: 0, startY: 4, nodes: []);
        grid.MoveTo(5, 4, cost: 5, nodes: []);

        grid.ResetToInitial(nodes: []);

        grid.PartyX.Should().Be(0);
        grid.PartyY.Should().Be(4);
        grid.MovementBudgetRemaining.Should().Be(20);
        grid.RevealedCells.Should().NotContain((5, 4));
    }

    [Fact]
    public void Rehydrate_ShouldRestoreExactState()
    {
        var nodeId = NodeId.New();

        var grid = RoomGrid.Rehydrate(
            width: 10, height: 8, movementBudget: 20, movementBudgetRemaining: 12,
            startX: 0, startY: 4, partyX: 3, partyY: 4,
            revealedNodeIds: [nodeId], revealedCells: [(3, 4), (2, 4)]);

        grid.MovementBudgetRemaining.Should().Be(12);
        grid.PartyX.Should().Be(3);
        grid.PartyY.Should().Be(4);
        grid.RevealedNodeIds.Should().ContainSingle().Which.Should().Be(nodeId);
        grid.RevealedCells.Should().BeEquivalentTo(new[] { (3, 4), (2, 4) });
    }
}
