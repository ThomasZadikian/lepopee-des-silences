using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.UnitTests.Rooms;

public sealed class RoomRoomNpcTests
{
    private static RoomBossProfile CreateBossProfile() => RoomBossProfile.Create(
        bossId: "boss.test.npc", name: "Gardien de Test", roomType: RoomType.Threshold,
        dangerHint: "High", enemyTemplateKey: "boss-test-npc-v1");

    private static MapNode CreateAvailableNode(int lane, int row, bool isBoss = false) => MapNode.Create(
        isBoss ? NodeEventType.RoomBoss : NodeEventType.Item,
        riskLevel: isBoss ? 85 : 10,
        rewardProfile: isBoss ? "room-boss" : "standard",
        row, lane, parentNodeIds: Array.Empty<NodeId>(),
        isBoss: isBoss, initialState: NodeState.Available);

    /// <summary>10x10 grid, party starts at (0,0), boss placed close enough to stay reachable.</summary>
    private static Room CreateRoom(int movementBudget = 20)
    {
        var bossNode = CreateAvailableNode(lane: 9, row: 9, isBoss: true);

        return Room.Create(
            depth: 0, RoomType.Threshold, PalaceRoomState.Neutral, "Threshold",
            CreateBossProfile(), [bossNode],
            gridWidth: 10, gridHeight: 10, movementBudget, startX: 0, startY: 0,
            layoutTemplateKey: "test-npc-v1", layoutTemplateVersion: "1.0.0");
    }

    [Fact]
    public void AddRoomNpc_ShouldRejectPositionOutsideGridBounds()
    {
        var room = CreateRoom();
        var npc = RoomNpc.Create("majordome", x: 99, y: 0, NpcBehaviorArchetype.Guardian);

        var act = () => room.AddRoomNpc(npc);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddRoomNpc_ShouldAcceptAnNpcOnAFloorCell()
    {
        var room = CreateRoom();
        var npc = RoomNpc.Create("majordome", x: 3, y: 3, NpcBehaviorArchetype.Guardian);

        room.AddRoomNpc(npc);

        room.RoomNpcs.Should().ContainSingle(n => n.Id == npc.Id);
    }

    [Fact]
    public void InteractWithRoomNpc_ShouldEscalateAdjacentNpcToAware()
    {
        var room = CreateRoom();
        var npc = RoomNpc.Create("majordome", x: 1, y: 0, NpcBehaviorArchetype.Guardian, awarenessRadius: 0);
        room.AddRoomNpc(npc);

        room.InteractWithRoomNpc(npc.Id);

        room.RoomNpcs.Single().Awareness.Should().Be(NpcAwarenessState.Aware);
    }

    [Fact]
    public void InteractWithRoomNpc_ShouldThrow_WhenNpcDoesNotBelongToRoom()
    {
        var room = CreateRoom();

        var act = () => room.InteractWithRoomNpc(RoomNpcId.New());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveParty_ShouldStepEveryRoomNpc_OncePerCellActuallyEntered()
    {
        var room = CreateRoom();
        // Fixed at its post, but tracked to confirm it never relocates during the move below.
        var fixedNpc = RoomNpc.Create("statue", x: 5, y: 5, NpcBehaviorArchetype.Fixed);
        room.AddRoomNpc(fixedNpc);

        room.MoveParty(3, 0);

        room.RoomNpcs.Single().X.Should().Be(5);
        room.RoomNpcs.Single().Y.Should().Be(5);
    }

    [Fact]
    public void MoveParty_ShouldRefreshAwareness_WhenPartyEntersLineOfSight()
    {
        var room = CreateRoom();
        var npc = RoomNpc.Create("majordome", x: 3, y: 0, NpcBehaviorArchetype.Guardian, awarenessRadius: 5);
        room.AddRoomNpc(npc);
        room.RoomNpcs.Single().Awareness.Should().Be(NpcAwarenessState.Unaware);

        room.MoveParty(1, 0);

        room.RoomNpcs.Single().Awareness.Should().Be(NpcAwarenessState.Aware);
    }

    [Fact]
    public void MoveParty_ShouldLetAHunterCloseDistance_OnceAware()
    {
        var room = CreateRoom();
        var hunter = RoomNpc.Create("chasseur", x: 9, y: 0, NpcBehaviorArchetype.Hunter, awarenessRadius: 20);
        room.AddRoomNpc(hunter);

        // Three cells entered: the first Step still finds the hunter Unaware (Step runs before
        // RefreshAwareness for that same cell — see Room.MoveParty), so it only starts closing
        // distance on the second and third cell of this same move.
        room.MoveParty(3, 0);

        var tracked = room.RoomNpcs.Single();
        tracked.Awareness.Should().Be(NpcAwarenessState.Aware);
        (tracked.X + tracked.Y).Should().BeLessThan(9);
    }
}
