using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Nodes;

public interface IRoomNodeFactory
{
    IReadOnlyCollection<Node> CreateLayerNodes(
        Random random,
        string seed,
        string matrixVersion,
        RoomType roomType,
        int roomDepth,
        int totalNodeCount,
        IRoomEventGenerationState eventGenerationState,
        int nodeDepth,
        int nodeCount,
        IReadOnlyCollection<Node> previousLayer,
        NodeState initialState);

    Node CreateBossNode(
        RoomType roomType,
        int nodeDepth,
        IReadOnlyCollection<NodeId> parentNodeIds);
}