using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Common.Factories;

public static class TestNodeEventResolutionContextFactory
{
    public static NodeEventResolutionContext Create(NodeEventType eventType)
    {
        var room = CreateRoom(eventType);

        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-event-resolution-test",
            "gen-0.2.0",
            "markov-0.2.0",
            room,
            DateTimeOffset.UtcNow);

        var node = room.AvailableNodes.Single();

        return new NodeEventResolutionContext(
            run,
            room,
            node);
    }

    private static Room CreateRoom(NodeEventType eventType)
    {
        var roomType = RoomType.Threshold;

        var bossProfile = RoomBossProfile.Create(
            "threshold-guardian",
            "Gardien du Seuil",
            roomType,
            "High");

        var startNode = Node.Create(
            eventType,
            riskLevel: 20,
            rewardProfile: "test-reward",
            nodeDepth: 0,
            initialState: NodeState.Available);

        var middleNode = Node.Create(
            NodeEventType.Combat,
            riskLevel: 30,
            rewardProfile: "combat-common",
            nodeDepth: 1,
            parentNodeId: startNode.Id,
            initialState: NodeState.Planned);

        var branchNodeA = Node.Create(
            NodeEventType.Item,
            riskLevel: 10,
            rewardProfile: "common",
            nodeDepth: 0,
            initialState: NodeState.Available);

        var branchNodeB = Node.Create(
            NodeEventType.Rest,
            riskLevel: 5,
            rewardProfile: "healing-only",
            nodeDepth: 1,
            parentNodeId: branchNodeA.Id,
            initialState: NodeState.Planned);

        var branchNodeC = Node.Create(
            NodeEventType.Rare,
            riskLevel: 40,
            rewardProfile: "rare",
            nodeDepth: 1,
            parentNodeId: branchNodeA.Id,
            initialState: NodeState.Planned);

        var bossNode = Node.Create(
            new[] { NodeEvent.Create(NodeEventType.RoomBoss, 1) },
            riskLevel: 80,
            rewardProfile: "room-boss",
            nodeDepth: 2,
            parentNodeIds: new[] { middleNode.Id, branchNodeB.Id, branchNodeC.Id },
            isRoomBossNode: true,
            initialState: NodeState.Planned);

        return Room.Create(
            0,
            roomType,
            "Threshold",
            bossProfile,
            new[]
            {
                startNode,
                branchNodeA,
                middleNode,
                branchNodeB,
                branchNodeC,
                bossNode
            });
    }
}