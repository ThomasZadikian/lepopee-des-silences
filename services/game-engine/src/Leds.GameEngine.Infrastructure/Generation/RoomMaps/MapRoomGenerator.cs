using Leds.GameEngine.Application.RoomMaps;
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

    public async Task<Room> GenerateAsync(
        string seed,
        string generatorVersion,
        int roomDepth,
        RoomType roomType,
        Random random,
        CancellationToken cancellationToken = default,
        PalaceRoomState palaceState = PalaceRoomState.Neutral)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seed);
        ArgumentException.ThrowIfNullOrWhiteSpace(generatorVersion);
        ArgumentNullException.ThrowIfNull(random);

        var template = _templateProvider.GetTemplate(roomType, generatorVersion);
        var profile = _generationProfileProvider.GetProfile(roomType);

        var nodes = CreateNodes(template, profile, random);
        ConnectNodes(nodes, template, random);

        var bossProfile = await _bossProfileResolver.ResolveAsync(roomType, cancellationToken);

        var room = Room.Create(
            roomDepth,
            roomType,
            palaceState,
            _themeResolver.Resolve(roomType),
            bossProfile,
            nodes);

        return Room.CreateFromTemplate(
            room.Depth,
            room.RoomType,
            room.PalaceState,
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
                    : NodeGenerationHeuristics.PickWeightedNodeType(profile, random);

                var riskLevel = isBossRow
                    ? 85
                    : random.Next(profile.RiskMin, profile.RiskMax);

                var combatRiskTier = NodeGenerationHeuristics.DeriveCombatRiskTier(type, riskLevel);

                var rewardProfile = isBossRow
                    ? "room-boss"
                    : NodeGenerationHeuristics.PickRewardProfile(type, profile, random);

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
                    initialState,
                    combatRiskTier: combatRiskTier));
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
}
