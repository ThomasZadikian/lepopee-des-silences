using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.UnitTests.Rooms;

public sealed class RoomGridLifecycleTests
{
    private static RoomBossProfile CreateBossProfile() => RoomBossProfile.Create(
        bossId: "boss.test.grid", name: "Gardien de Test", roomType: RoomType.Threshold,
        dangerHint: "High", enemyTemplateKey: "boss-test-grid-v1");

    private static MapNode CreateAvailableNode(int lane, int row, bool isBoss = false) => MapNode.Create(
        isBoss ? NodeEventType.RoomBoss : NodeEventType.Item,
        riskLevel: isBoss ? 85 : 10,
        rewardProfile: isBoss ? "room-boss" : "standard",
        row, lane, parentNodeIds: Array.Empty<NodeId>(),
        isBoss: isBoss, initialState: NodeState.Available);

    /// <summary>Small 5x5 grid, party starts at (0,0), boss at (4,4) — distance 8, budget 10.</summary>
    private static Room CreateGridRoom(int movementBudget = 10)
    {
        var itemNode = CreateAvailableNode(lane: 1, row: 0);
        var bossNode = CreateAvailableNode(lane: 4, row: 4, isBoss: true);

        return Room.Create(
            depth: 0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold",
            CreateBossProfile(), [itemNode, bossNode],
            gridWidth: 5, gridHeight: 5, movementBudget, startX: 0, startY: 0,
            layoutTemplateKey: "test-grid-v1", layoutTemplateVersion: "1.0.0");
    }

    [Fact]
    public void CreateGrid_ShouldBuildARoomWithAGrid()
    {
        var room = CreateGridRoom();

        room.Grid.Should().NotBeNull();
        room.State.Should().Be(RoomState.Active);
        room.Nodes.Should().HaveCount(2);
    }

    [Fact]
    public void CreateGrid_ShouldAllowRoomWithoutBoss_WhenProfileIsAlsoAbsent()
    {
        var itemNode = CreateAvailableNode(lane: 1, row: 0);

        var act = () => Room.Create(
            0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold", bossProfile: null,
            [itemNode], gridWidth: 5, gridHeight: 5, movementBudget: 10, startX: 0, startY: 0,
            layoutTemplateKey: "k", layoutTemplateVersion: "v");

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateGrid_ShouldRejectNodeOutsideGridBounds()
    {
        var outOfBounds = CreateAvailableNode(lane: 99, row: 0);
        var bossNode = CreateAvailableNode(lane: 4, row: 4, isBoss: true);

        var act = () => Room.Create(
            0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold", CreateBossProfile(),
            [outOfBounds, bossNode], gridWidth: 5, gridHeight: 5, movementBudget: 10, startX: 0, startY: 0,
            layoutTemplateKey: "k", layoutTemplateVersion: "v");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateGrid_ShouldRejectTwoNodesOnTheSameCell()
    {
        var nodeA = CreateAvailableNode(lane: 1, row: 1);
        var nodeB = CreateAvailableNode(lane: 1, row: 1);
        var bossNode = CreateAvailableNode(lane: 4, row: 4, isBoss: true);

        var act = () => Room.Create(
            0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold", CreateBossProfile(),
            [nodeA, nodeB, bossNode], gridWidth: 5, gridHeight: 5, movementBudget: 10, startX: 0, startY: 0,
            layoutTemplateKey: "k", layoutTemplateVersion: "v");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateGrid_ShouldAllowBossBeyondLegacyMovementBudget()
    {
        var bossNode = CreateAvailableNode(lane: 4, row: 4, isBoss: true);

        var act = () => Room.Create(
            0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold", CreateBossProfile(),
            [bossNode], gridWidth: 5, gridHeight: 5, movementBudget: 1, startX: 0, startY: 0,
            layoutTemplateKey: "k", layoutTemplateVersion: "v");

        act.Should().NotThrow();
    }

    [Fact]
    public void MoveParty_ShouldMoveWithoutConsumingGlobalBudget()
    {
        var room = CreateGridRoom();

        room.MoveParty(1, 0);

        room.Grid!.PartyX.Should().Be(1);
        room.Grid.PartyY.Should().Be(0);
        room.Grid.MovementBudgetRemaining.Should().Be(10);
    }

    [Fact]
    public void MoveParty_ShouldRemainPossibleRegardlessOfLegacyBudget()
    {
        var room = CreateGridRoom(movementBudget: 10);
        room.MoveParty(4, 4); // costs 8 (start to boss), remaining = 2

        var act = () => room.MoveParty(0, 0); // costs 8 back to start, only 2 remaining

        act.Should().NotThrow();
    }

    [Fact]
    public void MoveParty_ShouldThrow_WhenTargetOutsideBounds()
    {
        var room = CreateGridRoom();

        var act = () => room.MoveParty(10, 10);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EnterNodeAtPartyPosition_ShouldSelectTheNode_WhenPartyIsOnItsCell()
    {
        var room = CreateGridRoom();
        var itemNode = room.Nodes.Single(n => !n.IsBoss);
        room.MoveParty(itemNode.Lane, itemNode.Row);

        room.EnterNodeAtPartyPosition(itemNode.Id);

        room.State.Should().Be(RoomState.NodeSelected);
        itemNode.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void EnterNodeAtPartyPosition_ShouldThrow_WhenPartyIsNotOnItsCell()
    {
        var room = CreateGridRoom();
        var itemNode = room.Nodes.Single(n => !n.IsBoss);

        var act = () => room.EnterNodeAtPartyPosition(itemNode.Id);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ResolveSelectedGridNodeEvent_ShouldResolveNonBossNode_AndReturnToNodeResolved()
    {
        var room = CreateGridRoom();
        var itemNode = room.Nodes.Single(n => !n.IsBoss);
        room.MoveParty(itemNode.Lane, itemNode.Row);
        room.EnterNodeAtPartyPosition(itemNode.Id);

        room.ResolveSelectedNodeEvent();

        room.State.Should().Be(RoomState.NodeResolved);
        itemNode.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void ResolveSelectedGridNodeEvent_ShouldResolveTheRoom_WhenBossIsResolved()
    {
        // The boss no longer locks the room shut once defeated (see Run.ConfirmRoomExit) —
        // resolving it is just another resolved node, same as any other.
        var room = CreateGridRoom();
        var boss = room.Nodes.Single(n => n.IsBoss);
        room.MoveParty(boss.Lane, boss.Row);
        room.EnterNodeAtPartyPosition(boss.Id);

        room.ResolveSelectedNodeEvent();

        room.State.Should().Be(RoomState.NodeResolved);
    }

    [Fact]
    public void ReturnToGridExploration_ShouldReturnToActive()
    {
        var room = CreateGridRoom();
        var itemNode = room.Nodes.Single(n => !n.IsBoss);
        room.MoveParty(itemNode.Lane, itemNode.Row);
        room.EnterNodeAtPartyPosition(itemNode.Id);
        room.ResolveSelectedNodeEvent();

        room.ReturnToExploration();

        room.State.Should().Be(RoomState.Active);
    }

    // -----------------------------------------------------------------------
    // Regression: "Room is not waiting for party movement." after a 2nd node.
    // CurrentResolvedNode used to scan _nodes by NodeState alone in grid mode;
    // since a resolved grid node never reverts, a 2nd resolved node made that
    // scan match more than one node (SingleOrDefault threw), so
    // ReturnToGridExploration was never reached and the room got stuck in
    // NodeResolved forever — MoveParty then always threw.
    // -----------------------------------------------------------------------

    /// <summary>5x5 grid, party at (0,0), TWO non-boss nodes plus the boss.</summary>
    private static Room CreateGridRoomWithTwoItemNodes(int movementBudget = 10)
    {
        var firstNode = CreateAvailableNode(lane: 1, row: 0);
        var secondNode = CreateAvailableNode(lane: 2, row: 0);
        var bossNode = CreateAvailableNode(lane: 4, row: 4, isBoss: true);

        return Room.Create(
            depth: 0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold",
            CreateBossProfile(), [firstNode, secondNode, bossNode],
            gridWidth: 5, gridHeight: 5, movementBudget, startX: 0, startY: 0,
            layoutTemplateKey: "test-grid-v1", layoutTemplateVersion: "1.0.0");
    }

    private static void EnterMoveResolveAndReturn(Room room, MapNode node)
    {
        room.MoveParty(node.Lane, node.Row);
        room.EnterNodeAtPartyPosition(node.Id);
        room.ResolveSelectedNodeEvent();
        room.ReturnToExploration();
    }

    [Fact]
    public void CurrentResolvedNode_ShouldIdentifyOnlyTheJustResolvedNode_AfterASecondNodeIsResolved()
    {
        var room = CreateGridRoomWithTwoItemNodes();
        var firstNode = room.Nodes.First(n => !n.IsBoss);
        var secondNode = room.Nodes.Where(n => !n.IsBoss).Skip(1).First();
        EnterMoveResolveAndReturn(room, firstNode);

        room.MoveParty(secondNode.Lane, secondNode.Row);
        room.EnterNodeAtPartyPosition(secondNode.Id);
        room.ResolveSelectedNodeEvent();

        // Both nodes are Resolved by now — CurrentResolvedNode must still resolve to
        // exactly the second one (the one this interaction cycle is actually about),
        // not throw "Sequence contains more than one matching element".
        var act = () => room.CurrentResolvedNode;
        act.Should().NotThrow();
        room.CurrentResolvedNode.Should().Be(secondNode);
        firstNode.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void MoveParty_ShouldStillWork_AfterResolvingASecondNode()
    {
        var room = CreateGridRoomWithTwoItemNodes();
        var firstNode = room.Nodes.First(n => !n.IsBoss);
        var secondNode = room.Nodes.Where(n => !n.IsBoss).Skip(1).First();
        EnterMoveResolveAndReturn(room, firstNode);
        EnterMoveResolveAndReturn(room, secondNode);

        room.State.Should().Be(RoomState.Active);
        var act = () => room.MoveParty(0, 1);
        act.Should().NotThrow();
    }

    [Fact]
    public void ResetProgress_ShouldRestoreGridPositionAndNodeStates()
    {
        var room = CreateGridRoom();
        var itemNode = room.Nodes.Single(n => !n.IsBoss);
        room.MoveParty(itemNode.Lane, itemNode.Row);
        room.EnterNodeAtPartyPosition(itemNode.Id);
        room.ResolveSelectedNodeEvent();

        room.ResetProgress();

        room.State.Should().Be(RoomState.Active);
        room.Grid!.PartyX.Should().Be(0);
        room.Grid.PartyY.Should().Be(0);
        itemNode.State.Should().Be(NodeState.Available);
    }

}
