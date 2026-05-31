using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Events;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Rewards;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Risk;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Nodes;

public sealed class RoomNodeFactory : IRoomNodeFactory
{
    private readonly INodeEventGenerator _nodeEventGenerator;
    private readonly INodeRiskResolver _riskResolver;
    private readonly INodeRewardProfileResolver _rewardProfileResolver;

    public RoomNodeFactory(
        INodeEventGenerator nodeEventGenerator,
        INodeRiskResolver riskResolver,
        INodeRewardProfileResolver rewardProfileResolver)
    {
        _nodeEventGenerator = nodeEventGenerator;
        _riskResolver = riskResolver;
        _rewardProfileResolver = rewardProfileResolver;
    }

    public IReadOnlyCollection<Node> CreateLayerNodes(
        Random random,
        RoomType roomType,
        int totalNodeCount,
        IRoomEventGenerationState eventGenerationState,
        int nodeDepth,
        int nodeCount,
        IReadOnlyCollection<Node> previousLayer,
        NodeState initialState)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(eventGenerationState);
        ArgumentNullException.ThrowIfNull(previousLayer);

        var nodes = new List<Node>();

        var parentNodeIds = nodeDepth == 0
            ? Array.Empty<NodeId>()
            : previousLayer.Select(node => node.Id).ToArray();

        for (var index = 0; index < nodeCount; index++)
        {
            var events = _nodeEventGenerator.Generate(
                random,
                roomType,
                totalNodeCount,
                eventGenerationState);

            var riskLevel = _riskResolver.Resolve(events, random);
            var rewardProfile = _rewardProfileResolver.Resolve(events);

            nodes.Add(Node.Create(
                events,
                riskLevel,
                rewardProfile,
                nodeDepth,
                parentNodeIds,
                isRoomBossNode: false,
                initialState));
        }

        return nodes;
    }

    public Node CreateBossNode(
        RoomType roomType,
        int nodeDepth,
        IReadOnlyCollection<NodeId> parentNodeIds)
    {
        ArgumentNullException.ThrowIfNull(parentNodeIds);

        return Node.Create(
            new[]
            {
                NodeEvent.Create(NodeEventType.RoomBoss, order: 1)
            },
            riskLevel: roomType == RoomType.Final ? 100 : 85,
            rewardProfile: roomType == RoomType.Final ? "final-boss" : "room-boss",
            nodeDepth,
            parentNodeIds,
            isRoomBossNode: true,
            initialState: NodeState.Planned);
    }
}