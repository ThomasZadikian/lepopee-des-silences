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

        return Room.CreateGrid(
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
    public void CreateGrid_ShouldRejectMissingBossNode()
    {
        var itemNode = CreateAvailableNode(lane: 1, row: 0);

        var act = () => Room.CreateGrid(
            0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold", CreateBossProfile(),
            [itemNode], gridWidth: 5, gridHeight: 5, movementBudget: 10, startX: 0, startY: 0,
            layoutTemplateKey: "k", layoutTemplateVersion: "v");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateGrid_ShouldRejectNodeOutsideGridBounds()
    {
        var outOfBounds = CreateAvailableNode(lane: 99, row: 0);
        var bossNode = CreateAvailableNode(lane: 4, row: 4, isBoss: true);

        var act = () => Room.CreateGrid(
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

        var act = () => Room.CreateGrid(
            0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold", CreateBossProfile(),
            [nodeA, nodeB, bossNode], gridWidth: 5, gridHeight: 5, movementBudget: 10, startX: 0, startY: 0,
            layoutTemplateKey: "k", layoutTemplateVersion: "v");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateGrid_ShouldRejectBossUnreachableWithinMovementBudget()
    {
        var bossNode = CreateAvailableNode(lane: 4, row: 4, isBoss: true);

        var act = () => Room.CreateGrid(
            0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold", CreateBossProfile(),
            [bossNode], gridWidth: 5, gridHeight: 5, movementBudget: 1, startX: 0, startY: 0,
            layoutTemplateKey: "k", layoutTemplateVersion: "v");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveParty_ShouldMoveAndDeductBudget()
    {
        var room = CreateGridRoom();

        room.MoveParty(1, 0);

        room.Grid!.PartyX.Should().Be(1);
        room.Grid.PartyY.Should().Be(0);
        room.Grid.MovementBudgetRemaining.Should().Be(9);
    }

    [Fact]
    public void MoveParty_ShouldThrow_WhenNotEnoughBudget()
    {
        var room = CreateGridRoom(movementBudget: 10);
        room.MoveParty(4, 4); // costs 8 (start to boss), remaining = 2

        var act = () => room.MoveParty(0, 0); // costs 8 back to start, only 2 remaining

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveParty_ShouldThrow_WhenTargetOutsideBounds()
    {
        var room = CreateGridRoom();

        var act = () => room.MoveParty(10, 10);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveParty_ShouldThrow_OnAClassicRoom()
    {
        var room = CreateClassicRoom();

        var act = () => room.MoveParty(1, 0);

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

        room.ResolveSelectedGridNodeEvent();

        room.State.Should().Be(RoomState.NodeResolved);
        itemNode.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void ResolveSelectedGridNodeEvent_ShouldCompleteTheRoom_WhenBossIsResolved()
    {
        var room = CreateGridRoom();
        var boss = room.Nodes.Single(n => n.IsBoss);
        room.MoveParty(boss.Lane, boss.Row);
        room.EnterNodeAtPartyPosition(boss.Id);

        room.ResolveSelectedGridNodeEvent();

        room.State.Should().Be(RoomState.Completed);
    }

    [Fact]
    public void ReturnToGridExploration_ShouldReturnToActive()
    {
        var room = CreateGridRoom();
        var itemNode = room.Nodes.Single(n => !n.IsBoss);
        room.MoveParty(itemNode.Lane, itemNode.Row);
        room.EnterNodeAtPartyPosition(itemNode.Id);
        room.ResolveSelectedGridNodeEvent();

        room.ReturnToGridExploration();

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

        return Room.CreateGrid(
            depth: 0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold",
            CreateBossProfile(), [firstNode, secondNode, bossNode],
            gridWidth: 5, gridHeight: 5, movementBudget, startX: 0, startY: 0,
            layoutTemplateKey: "test-grid-v1", layoutTemplateVersion: "1.0.0");
    }

    private static void EnterMoveResolveAndReturn(Room room, MapNode node)
    {
        room.MoveParty(node.Lane, node.Row);
        room.EnterNodeAtPartyPosition(node.Id);
        room.ResolveSelectedGridNodeEvent();
        room.ReturnToGridExploration();
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
        room.ResolveSelectedGridNodeEvent();

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
    public void CanChallengeBossRemotely_ShouldBeFalse_WhenBudgetRemains()
    {
        var room = CreateGridRoom();

        room.CanChallengeBossRemotely.Should().BeFalse();
    }

    [Fact]
    public void CanChallengeBossRemotely_ShouldBeTrue_WhenBudgetIsExhausted()
    {
        var room = CreateGridRoom(movementBudget: 8);
        var boss = room.Nodes.Single(n => n.IsBoss);
        room.MoveParty(boss.Lane, boss.Row); // costs exactly 8

        room.CanChallengeBossRemotely.Should().BeTrue();
    }

    [Fact]
    public void ChallengeBossRemotely_ShouldSelectTheBoss_WithoutRequiringPartyToBeOnItsCell()
    {
        var room = CreateGridRoom(movementBudget: 8);
        room.MoveParty(4, 0); // costs 4, remaining 4
        room.MoveParty(0, 0); // costs 4 back to start, remaining 0 — nowhere near the boss

        room.ChallengeBossRemotely();

        room.State.Should().Be(RoomState.NodeSelected);
        room.Nodes.Single(n => n.IsBoss).State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void ChallengeBossRemotely_ShouldThrow_WhenBudgetStillRemains()
    {
        var room = CreateGridRoom();

        var act = room.ChallengeBossRemotely;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ResetProgress_ShouldRestoreGridPositionBudgetAndNodeStates()
    {
        var room = CreateGridRoom();
        var itemNode = room.Nodes.Single(n => !n.IsBoss);
        room.MoveParty(itemNode.Lane, itemNode.Row);
        room.EnterNodeAtPartyPosition(itemNode.Id);
        room.ResolveSelectedGridNodeEvent();

        room.ResetProgress();

        room.State.Should().Be(RoomState.Active);
        room.Grid!.PartyX.Should().Be(0);
        room.Grid.PartyY.Should().Be(0);
        room.Grid.MovementBudgetRemaining.Should().Be(room.Grid.MovementBudget);
        itemNode.State.Should().Be(NodeState.Available);
    }

    // -----------------------------------------------------------------------
    // Classic-mode room must reject the grid-only methods (defense-in-depth)
    // -----------------------------------------------------------------------

    private static Room CreateClassicRoom()
    {
        var boss = MapNode.Create(
            NodeEventType.RoomBoss, riskLevel: 85, rewardProfile: "room-boss",
            row: 1, lane: 0, parentNodeIds: Array.Empty<NodeId>(),
            isBoss: true, initialState: NodeState.Planned);
        var initial = MapNode.Create(
            NodeEventType.Item, riskLevel: 10, rewardProfile: "standard",
            row: 0, lane: 0, parentNodeIds: Array.Empty<NodeId>(),
            isBoss: false, initialState: NodeState.Available);
        boss.AddParent(initial.Id);

        return Room.Create(0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold", CreateBossProfile(), [initial, boss]);
    }

    [Fact]
    public void EnterNodeAtPartyPosition_ShouldThrow_OnAClassicRoom()
    {
        var room = CreateClassicRoom();
        var node = room.Nodes.First();

        var act = () => room.EnterNodeAtPartyPosition(node.Id);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SelectNode_ShouldThrow_OnAGridRoom()
    {
        var room = CreateGridRoom();
        var itemNode = room.Nodes.Single(n => !n.IsBoss);

        var act = () => room.SelectNode(itemNode.Id);

        act.Should().Throw<DomainException>();
    }
}
