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
    private readonly IRoomTypeGenerationProfileProvider _generationProfileProvider;

    public MapRoomGenerator(
        IRoomMapLayoutTemplateProvider templateProvider,
        IRoomThemeResolver themeResolver,
        IRoomBossProfileResolver bossProfileResolver,
        IRoomTypeGenerationProfileProvider generationProfileProvider)
    {
        _templateProvider = templateProvider;
        _themeResolver = themeResolver;
        _bossProfileResolver = bossProfileResolver;
        _generationProfileProvider = generationProfileProvider;
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
        var profile = _generationProfileProvider.GetProfile(roomType);

        var nodes = CreateNodes(template, profile, random);
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

    private static List<MapNode> CreateNodes(
        RoomMapLayoutTemplate template,
        RoomTypeGenerationProfile profile,
        Random random)
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
                    : PickWeightedNodeType(profile, random);

                var riskLevel = isBossRow
                    ? 85
                    : random.Next(profile.RiskMin, profile.RiskMax);

                var rewardProfile = isBossRow
                    ? "room-boss"
                    : PickRewardProfile(type, profile, random);

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

    /// <summary>
    /// Selects a node type using the profile's weighted distribution.
    /// </summary>
    private static NodeEventType PickWeightedNodeType(RoomTypeGenerationProfile profile, Random random)
    {
        var roll = random.Next(profile.TotalWeight);
        var cumulative = 0;

        foreach (var weight in profile.NodeTypeWeights)
        {
            cumulative += weight.Weight;
            if (roll < cumulative)
            {
                return weight.NodeType;
            }
        }

        // Fallback — unreachable if TotalWeight is consistent.
        return profile.NodeTypeWeights[0].NodeType;
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

    /// <summary>
    /// Picks a reward profile for the given node type using the room profile's rules.
    /// Falls back to static defaults when no rule is defined.
    /// </summary>
    private static string PickRewardProfile(
        NodeEventType type,
        RoomTypeGenerationProfile profile,
        Random random)
    {
        if (profile.RewardProfilesByNodeType.TryGetValue(type, out var options) && options.Count > 0)
        {
            return options.Count == 1 ? options[0] : options[random.Next(options.Count)];
        }

        // Static fallback — reached only for node types absent from the profile rules.
        return type switch
        {
            NodeEventType.Combat   => "combat-common",
            NodeEventType.Elite    => "elite",
            NodeEventType.Rest     => "rest-safe",
            NodeEventType.Item     => "item-common",
            NodeEventType.Npc      => "narrative",
            NodeEventType.Merchant => "merchant",
            NodeEventType.Law      => "law",
            NodeEventType.Curse    => "curse",
            NodeEventType.Rare     => "rare",
            _                      => "standard"
        };
    }
}
