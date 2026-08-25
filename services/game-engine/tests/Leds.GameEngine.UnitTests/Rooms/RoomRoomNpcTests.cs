using FluentAssertions;
using Leds.GameEngine.Domain.Actors;
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
    public void AddRoomNpc_ShouldRejectThePartyCell_AndAnotherNpcCell()
    {
        var room = CreateRoom();
        room.AddRoomNpc(RoomNpc.Create("majordome", x: 2, y: 0, NpcBehaviorArchetype.Guardian));

        var onParty = () => room.AddRoomNpc(
            RoomNpc.Create("chien", x: 0, y: 0, NpcBehaviorArchetype.Passive));
        var onNpc = () => room.AddRoomNpc(
            RoomNpc.Create("visiteur", x: 2, y: 0, NpcBehaviorArchetype.Passive));

        onParty.Should().Throw<DomainException>();
        onNpc.Should().Throw<DomainException>();
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
    public void MoveParty_ShouldNotMoveRoomNpcsWhileThePartyIsMoving()
    {
        var room = CreateRoom();
        var passiveNpc = RoomNpc.Create("habitant", x: 5, y: 5, NpcBehaviorArchetype.Passive);
        room.AddRoomNpc(passiveNpc);

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
    public void MoveParty_ShouldRouteAroundAnOccupiedNpcCell()
    {
        var room = CreateRoom();
        room.AddRoomNpc(RoomNpc.Create(
            "majordome", x: 1, y: 0, NpcBehaviorArchetype.Guardian));

        var result = room.MoveParty(2, 0);

        result.TraversedCells.Should().NotContain((1, 0));
        (room.Grid.PartyX, room.Grid.PartyY).Should().Be((2, 0));
    }

    [Fact]
    public void MoveParty_ShouldRejectATargetCellOccupiedByAnNpc()
    {
        var room = CreateRoom();
        room.AddRoomNpc(RoomNpc.Create(
            "majordome", x: 1, y: 0, NpcBehaviorArchetype.Guardian));

        var act = () => room.MoveParty(1, 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AdvanceActors_ShouldKeepANeutralNpcStillWithinTwoCells()
    {
        var room = CreateRoom();
        var npc = RoomNpc.Create("habitant", x: 2, y: 0, NpcBehaviorArchetype.Passive);
        room.AddRoomNpc(npc);

        var result = room.AdvanceActors(ActorAdvanceMode.All);

        result.Movements.Should().NotContain(movement => movement.ActorKind == ActorKind.Npc);
        (npc.X, npc.Y).Should().Be((2, 0));
    }
}
