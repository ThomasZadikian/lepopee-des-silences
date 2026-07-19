using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

public sealed class GridRoomGenerator : IGridRoomGenerator
{
    private readonly IGridRoomLayoutTemplateProvider _templateProvider;
    private readonly IRoomThemeResolver _themeResolver;
    private readonly IRoomBossProfileResolver _bossProfileResolver;
    private readonly IRoomTypeGenerationProfileProvider _generationProfileProvider;

    public GridRoomGenerator(
        IGridRoomLayoutTemplateProvider templateProvider,
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

        var bossProfile = await _bossProfileResolver.ResolveAsync(roomType, cancellationToken);

        return Room.CreateGrid(
            roomDepth,
            roomType,
            palaceState,
            _themeResolver.Resolve(roomType),
            bossProfile,
            nodes,
            template.Width,
            template.Height,
            template.MovementBudget,
            template.StartX,
            template.StartY,
            template.Key,
            template.Version);
    }

    private static List<MapNode> CreateNodes(
        GridRoomLayoutTemplate template,
        RoomTypeGenerationProfile profile,
        Random random)
    {
        var (bossX, bossY) = FindFarthestCell(template.StartX, template.StartY, template.Width, template.Height);

        var occupiedCells = new HashSet<(int X, int Y)> { (template.StartX, template.StartY), (bossX, bossY) };

        var nodeCount = random.Next(template.MinNodeCount, template.MaxNodeCount + 1);
        var otherNodeCount = nodeCount - 1;

        var freeCells = new List<(int X, int Y)>();
        for (var x = 0; x < template.Width; x++)
        {
            for (var y = 0; y < template.Height; y++)
            {
                if (!occupiedCells.Contains((x, y)))
                {
                    freeCells.Add((x, y));
                }
            }
        }

        var chosenCells = freeCells
            .OrderBy(_ => random.Next())
            .Take(Math.Min(otherNodeCount, freeCells.Count))
            .ToList();

        var nodes = new List<MapNode>();

        foreach (var (x, y) in chosenCells)
        {
            var type = NodeGenerationHeuristics.PickWeightedNodeType(profile, random);
            var riskLevel = random.Next(profile.RiskMin, profile.RiskMax);
            var combatRiskTier = NodeGenerationHeuristics.DeriveCombatRiskTier(type, riskLevel);
            var rewardProfile = NodeGenerationHeuristics.PickRewardProfile(type, profile, random);

            nodes.Add(MapNode.Create(
                eventType: type,
                riskLevel,
                rewardProfile,
                row: y,
                lane: x,
                parentNodeIds: Array.Empty<NodeId>(),
                isBoss: false,
                initialState: NodeState.Available,
                combatRiskTier: combatRiskTier));
        }

        nodes.Add(MapNode.Create(
            eventType: NodeEventType.RoomBoss,
            riskLevel: 85,
            rewardProfile: "room-boss",
            row: bossY,
            lane: bossX,
            parentNodeIds: Array.Empty<NodeId>(),
            isBoss: true,
            initialState: NodeState.Available,
            combatRiskTier: NodeGenerationHeuristics.DeriveCombatRiskTier(NodeEventType.RoomBoss, 85)));

        return nodes;
    }

    /// <summary>
    /// Deterministic given (startX, startY, width, height) — does not consume the seeded
    /// <see cref="Random"/>, so the boss's position only depends on the template's shape, not
    /// on the room's roll sequence. Ties broken by (X desc, Y desc) for determinism.
    /// </summary>
    private static (int X, int Y) FindFarthestCell(int startX, int startY, int width, int height)
    {
        var best = (X: startX, Y: startY);
        var bestDistance = -1;

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var distance = Math.Abs(x - startX) + Math.Abs(y - startY);

                if (distance > bestDistance
                    || (distance == bestDistance && (x > best.X || (x == best.X && y > best.Y))))
                {
                    best = (x, y);
                    bestDistance = distance;
                }
            }
        }

        return best;
    }
}
