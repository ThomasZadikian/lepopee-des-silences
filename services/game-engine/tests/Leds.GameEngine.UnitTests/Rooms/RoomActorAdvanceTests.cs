using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.UnitTests.Rooms;

public sealed class RoomActorAdvanceTests
{
    private static Room CreateRoom(MapNode hostile)
    {
        return Room.Create(
            depth: 0,
            roomType: RoomType.Threshold,
            palaceState: PalaceRoomState.Neutral,
            theme: "Threshold",
            bossProfile: null,
            nodes: [hostile],
            gridWidth: 6,
            gridHeight: 4,
            movementBudget: 20,
            startX: 0,
            startY: 0,
            layoutTemplateKey: "actor-advance-test",
            layoutTemplateVersion: "1.0.0");
    }

    private static MapNode CreateHostile(int x, int y) => MapNode.Create(
        NodeEventType.Combat,
        riskLevel: 30,
        rewardProfile: "combat-test",
        row: y,
        lane: x,
        parentNodeIds: Array.Empty<NodeId>(),
        contactBehavior: ContactBehavior.None);

    [Fact]
    public void AdvanceActors_ShouldPursueWhenHostileIsThreeCellsAway()
    {
        var hostile = CreateHostile(3, 0);
        var room = CreateRoom(hostile);

        var result = room.AdvanceActors(ActorAdvanceMode.HostilesOnly);

        result.Movements.Should().ContainSingle();
        (hostile.Lane, hostile.Row).Should().Be((2, 0));
        room.State.Should().Be(RoomState.Active);
    }

    [Fact]
    public void AdvanceActors_ShouldSelectAnAdjacentHostileByContact()
    {
        var hostile = CreateHostile(1, 0);
        var room = CreateRoom(hostile);

        var result = room.AdvanceActors(ActorAdvanceMode.HostilesOnly);

        result.TriggeredNodeId.Should().Be(hostile.Id);
        room.State.Should().Be(RoomState.NodeSelected);
        hostile.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void CombatNodes_ShouldAlwaysUseContactEvenForHistoricalNoneValues()
    {
        var hostile = CreateHostile(3, 0);

        hostile.TriggersOnContact.Should().BeTrue();
        hostile.BlocksTransit.Should().BeTrue();
    }
}
