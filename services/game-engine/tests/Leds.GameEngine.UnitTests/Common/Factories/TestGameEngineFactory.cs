using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Common.Factories;

public static class TestGameEngineFactory
{
    public static MapNode CreateMapNode(
        NodeEventType eventType = NodeEventType.Combat,
        int riskLevel = 25,
        string rewardProfile = "standard",
        int row = 0,
        int lane = 0,
        IReadOnlyCollection<NodeId>? parentNodeIds = null,
        bool isBoss = false,
        NodeState initialState = NodeState.Available)
    {
        return MapNode.Create(
            eventType,
            riskLevel,
            rewardProfile,
            row,
            lane,
            parentNodeIds ?? Array.Empty<NodeId>(),
            isBoss,
            initialState);
    }

    public static Room CreateThresholdRoom(
        NodeEventType targetInitialEventType = NodeEventType.Combat,
        int depth = 0)
    {
        return CreateThresholdRoomWithTargetInitialNode(targetInitialEventType, depth).Room;
    }

    public static TestRoomWithTargetNode CreateThresholdRoomWithTargetInitialNode(
        NodeEventType targetInitialEventType,
        int depth = 0)
    {
        var roomType = RoomType.Threshold;

        var bossProfile = RoomBossProfile.Create(
            bossId: "boss-threshold-v1",
            name: "Threshold Guardian",
            roomType: roomType,
            dangerHint: "A first guardian watches the threshold.");

        var targetNode = CreateMapNode(
            eventType: targetInitialEventType,
            riskLevel: 25,
            rewardProfile: "standard",
            row: 0,
            lane: 0,
            parentNodeIds: Array.Empty<NodeId>(),
            isBoss: false,
            initialState: NodeState.Available);

        var alternativeInitialNode = CreateMapNode(
            eventType: NodeEventType.Item,
            riskLevel: 10,
            rewardProfile: "standard",
            row: 0,
            lane: 1,
            parentNodeIds: Array.Empty<NodeId>(),
            isBoss: false,
            initialState: NodeState.Available);

        var progressionNodeFromTarget = CreateMapNode(
            eventType: NodeEventType.Combat,
            riskLevel: 30,
            rewardProfile: "combat-common",
            row: 1,
            lane: 0,
            parentNodeIds: new[] { targetNode.Id },
            isBoss: false,
            initialState: NodeState.Planned);

        var progressionNodeFromAlternativeA = CreateMapNode(
            eventType: NodeEventType.Rest,
            riskLevel: 5,
            rewardProfile: "healing-only",
            row: 1,
            lane: 1,
            parentNodeIds: new[] { alternativeInitialNode.Id },
            isBoss: false,
            initialState: NodeState.Planned);

        var progressionNodeFromAlternativeB = CreateMapNode(
            eventType: NodeEventType.Rare,
            riskLevel: 40,
            rewardProfile: "rare",
            row: 1,
            lane: 2,
            parentNodeIds: new[] { alternativeInitialNode.Id },
            isBoss: false,
            initialState: NodeState.Planned);

        var bossNode = CreateMapNode(
            eventType: NodeEventType.RoomBoss,
            riskLevel: 85,
            rewardProfile: "room-boss",
            row: 2,
            lane: 1,
            parentNodeIds: new[]
            {
                progressionNodeFromTarget.Id,
                progressionNodeFromAlternativeA.Id,
                progressionNodeFromAlternativeB.Id
            },
            isBoss: true,
            initialState: NodeState.Planned);

        var room = Room.Create(
            depth: depth,
            roomType: roomType,
            theme: "Threshold",
            bossProfile: bossProfile,
            nodes: new[]
            {
                targetNode,
                alternativeInitialNode,
                progressionNodeFromTarget,
                progressionNodeFromAlternativeA,
                progressionNodeFromAlternativeB,
                bossNode
            });

        return new TestRoomWithTargetNode(room, targetNode);
    }

    public static Run CreateRun(
        NodeEventType targetInitialEventType = NodeEventType.Combat)
    {
        var room = CreateThresholdRoom(targetInitialEventType);

        return Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-unit-test",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow);
    }

    public static TestRunWithTargetNode CreateRunWithTargetInitialNode(
        NodeEventType targetInitialEventType)
    {
        var roomWithTargetNode = CreateThresholdRoomWithTargetInitialNode(
            targetInitialEventType);

        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-unit-test-target-node",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: roomWithTargetNode.Room,
            startedAt: DateTimeOffset.UtcNow);

        return new TestRunWithTargetNode(run, roomWithTargetNode.TargetNode);
    }

    public static TestRunWithTargetNode CreateRunWithSelectedTargetNode(
        NodeEventType targetInitialEventType)
    {
        var runWithTargetNode = CreateRunWithTargetInitialNode(
            targetInitialEventType);

        runWithTargetNode.Run.ChooseNode(runWithTargetNode.TargetNode.Id);

        return runWithTargetNode;
    }

    public static TestRunWithTargetNode CreateRunWithResolvedCurrentEvent(
        NodeEventType targetInitialEventType)
    {
        var runWithTargetNode = CreateRunWithSelectedTargetNode(
            targetInitialEventType);

        runWithTargetNode.Run.ResolveCurrentEvent();

        return runWithTargetNode;
    }

    public static Run CreateRunWithCompletedCurrentRoom()
    {
        var run = CreateRun();

        while (run.Status == RunStatus.Active)
        {
            var node = run.CurrentRoom.AvailableNodes.First();

            run.ChooseNode(node.Id);
            run.ResolveCurrentEvent();

            if (run.Status == RunStatus.RoomResolved)
            {
                break;
            }

            run.ProgressCurrentRoom();
        }

        return run;
    }
}

public sealed record TestRoomWithTargetNode(
    Room Room,
    MapNode TargetNode);

public sealed record TestRunWithTargetNode(
    Run Run,
    MapNode TargetNode);
