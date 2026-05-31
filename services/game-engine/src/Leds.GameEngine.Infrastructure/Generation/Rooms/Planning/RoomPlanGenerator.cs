using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Common;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Events;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Layers;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Nodes;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Planning;

public sealed class RoomPlanGenerator : IRoomPlanGenerator
{
    private readonly IRoomBossProfileResolver _bossProfileResolver;
    private readonly IRoomThemeResolver _themeResolver;
    private readonly IRoomEventGenerationStateFactory _eventGenerationStateFactory;
    private readonly IRoomNodeLayerPlanner _layerPlanner;
    private readonly IRoomNodeFactory _nodeFactory;

    public RoomPlanGenerator(
        IRoomBossProfileResolver bossProfileResolver,
        IRoomThemeResolver themeResolver,
        IRoomEventGenerationStateFactory eventGenerationStateFactory,
        IRoomNodeLayerPlanner layerPlanner,
        IRoomNodeFactory nodeFactory)
    {
        _bossProfileResolver = bossProfileResolver;
        _themeResolver = themeResolver;
        _eventGenerationStateFactory = eventGenerationStateFactory;
        _layerPlanner = layerPlanner;
        _nodeFactory = nodeFactory;
    }

    public Room Generate(int roomDepth, RoomType roomType, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var totalNodeCount = random.Next(
            RoomGenerationConstants.MinRoomNodeCount,
            RoomGenerationConstants.MaxRoomNodeCount + 1);

        var nodes = GenerateNodes(random, roomType, totalNodeCount);

        return Room.Create(
            roomDepth,
            roomType,
            _themeResolver.Resolve(roomType),
            _bossProfileResolver.Resolve(roomType),
            nodes);
    }

    private IReadOnlyCollection<Node> GenerateNodes(
        Random random,
        RoomType roomType,
        int totalNodeCount)
    {
        var normalNodeCount = totalNodeCount - RoomGenerationConstants.BossNodeCount;
        var normalLayerCount = _layerPlanner.ResolveNormalLayerCount(random, normalNodeCount);

        var nodes = new List<Node>();
        var eventGenerationState = _eventGenerationStateFactory.Create();
        var remainingNormalNodes = normalNodeCount;

        IReadOnlyCollection<Node> previousLayer = Array.Empty<Node>();

        for (var nodeDepth = 0; nodeDepth < normalLayerCount; nodeDepth++)
        {
            var nodeCount = _layerPlanner.ResolveNodeCountForLayer(
                random,
                nodeDepth,
                normalLayerCount,
                remainingNormalNodes);

            var layerNodes = _nodeFactory.CreateLayerNodes(
                random,
                roomType,
                totalNodeCount,
                eventGenerationState,
                nodeDepth,
                nodeCount,
                previousLayer,
                nodeDepth == 0 ? NodeState.Available : NodeState.Planned);

            nodes.AddRange(layerNodes);

            previousLayer = layerNodes;
            remainingNormalNodes -= nodeCount;
        }

        if (remainingNormalNodes != 0)
        {
            throw new InvalidOperationException(
                "Room plan generation failed to allocate all normal nodes.");
        }

        if (previousLayer.Count == 0)
        {
            throw new InvalidOperationException(
                "Room plan generation failed to create a boss parent layer.");
        }

        var bossDepth = normalLayerCount;

        nodes.Add(_nodeFactory.CreateBossNode(
            roomType,
            bossDepth,
            previousLayer.Select(node => node.Id).ToArray()));

        return nodes;
    }
}