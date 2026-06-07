using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

public sealed class MapRoomGenerator : IMapRoomGenerator
{
    private readonly IRoomMapLayoutTemplateProvider _templateProvider;
    private readonly IRoomThemeResolver _themeResolver;
    private readonly IRoomBossProfileResolver _bossProfileResolver;

    private static readonly NodeEventType[] NormalNodeTypes =
    [
        NodeEventType.Combat,
        NodeEventType.Rest,
        NodeEventType.Item,
        NodeEventType.Npc,
        NodeEventType.Merchant,
        NodeEventType.Law,
        NodeEventType.Curse,
        NodeEventType.Rare,
        NodeEventType.Elite
    ];

    public MapRoomGenerator(
        IRoomMapLayoutTemplateProvider templateProvider,
        IRoomThemeResolver themeResolver,
        IRoomBossProfileResolver bossProfileResolver)
    {
        _templateProvider = templateProvider;
        _themeResolver = themeResolver;
        _bossProfileResolver = bossProfileResolver;
    }

    public Room Generate(
        string seed,
        string generatorVersion,
        int roomDepth,
        RoomType roomType,
        Random random)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seed);
        ArgumentException.ThrowIfNullOrWhiteSpace(generatorVersion);
        ArgumentNullException.ThrowIfNull(random);

        var template = _templateProvider.GetTemplate(roomType, generatorVersion);

        var nodes = CreateNodes(template, random);
        ConnectNodes(nodes, template, random);

        var room = Room.Create(
            roomDepth,
            roomType,
            _themeResolver.Resolve(roomType),
            _bossProfileResolver.Resolve(roomType),
            nodes);

        return Room.CreateFromTemplate(
            room.Depth,
            room.RoomType,
            room.Theme,
            room.BossProfile,
            room.Nodes,
            template.Key,
            template.Version);
    }

    private static List<MapNode> CreateNodes(RoomMapLayoutTemplate template, Random random)
    {
        var nodes = new List<MapNode>();

        for (var row = 0; row <= template.BossRowIndex; row++)
        {
            var nodeCount = template.RowNodeCounts[row];
            var isBossRow = row == template.BossRowIndex;

            for (var lane = 0; lane < nodeCount; lane++)
            {
                var type = isBossRow
                    ? NodeEventType.RoomBoss
                    : NormalNodeTypes[random.Next(NormalNodeTypes.Length)];

                var riskLevel = isBossRow ? 85 : random.Next(5, 76);
                var rewardProfile = isBossRow
                    ? "room-boss"
                    : PickRewardProfile(type, random);

                var parentNodeIds = Array.Empty<NodeId>();
                var initialState = row == 0 ? NodeState.Available : NodeState.Planned;

            nodes.Add(MapNode.Create(
                eventType: type,
                riskLevel,
                rewardProfile,
                row,
                lane,
                parentNodeIds,
                isBoss: isBossRow,
                initialState));
            }
        }

        return nodes;
    }

    private static void ConnectNodes(
        List<MapNode> nodes,
        RoomMapLayoutTemplate template,
        Random random)
    {
        var nodesByRow = nodes
            .GroupBy(n => n.Row)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Lane).ToList());

        for (var row = 0; row < template.BossRowIndex; row++)
        {
            var currentRowNodes = nodesByRow[row];
            var nextRowNodes = nodesByRow[row + 1];

            foreach (var parent in currentRowNodes)
            {
                var eligibleChildren = nextRowNodes
                    .Where(child => Math.Abs(child.Lane - parent.Lane) <= 1)
                    .ToList();

                if (eligibleChildren.Count == 0)
                {
                    continue;
                }

                var minConnections = 1;
                var maxConnections = eligibleChildren.Count;

                var connectionCount = random.Next(minConnections, maxConnections + 1);

                var shuffled = eligibleChildren
                    .OrderBy(_ => random.Next())
                    .Take(connectionCount)
                    .ToList();

                foreach (var child in shuffled)
                {
                    child.AddParent(parent.Id);
                }
            }

            foreach (var child in nextRowNodes.Where(n => n.ParentNodeIds.Count == 0))
            {
                var eligibleParents = currentRowNodes
                    .Where(p => Math.Abs(child.Lane - p.Lane) <= 1)
                    .ToList();

                if (eligibleParents.Count > 0)
                {
                    var parent = eligibleParents[random.Next(eligibleParents.Count)];
                    child.AddParent(parent.Id);
                }
            }
        }
    }

    private static string PickRewardProfile(NodeEventType type, Random random)
    {
        return type switch
        {
            NodeEventType.Combat => random.Next(2) == 0 ? "combat-common" : "combat-uncommon",
            NodeEventType.Elite => "elite",
            NodeEventType.Rest => "healing-only",
            NodeEventType.Item => random.Next(2) == 0 ? "item-common" : "item-uncommon",
            NodeEventType.Npc => "narrative",
            NodeEventType.Merchant => "merchant",
            NodeEventType.Law => "law",
            NodeEventType.Curse => "curse",
            NodeEventType.Rare => "rare",
            _ => "standard"
        };
    }
}
