using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.RoomMapLayouts;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

public sealed class GridRoomGenerator : IGridRoomGenerator
{
    // BALANCE KNOB — fraction of otherwise-free cells (not start/boss/node) that become
    // impassable obstacles. Every candidate is connectivity-checked before being kept (see
    // GenerateObstacles), so this is a target, not a guarantee — a very small/dense board may
    // end up with fewer.
    private const double ObstacleDensity = 0.15;

    private static readonly (int Dx, int Dy)[] Neighbors = { (1, 0), (-1, 0), (0, 1), (0, -1) };

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

        var occupiedCells = new List<(int X, int Y)> { (template.StartX, template.StartY) };
        occupiedCells.AddRange(nodes.Select(node => (node.Lane, node.Row)));

        var elevation = GenerateElevation(template.Width, template.Height, template.StartX, template.StartY, random);
        var obstacles = GenerateObstacles(
            template.Width, template.Height, template.StartX, template.StartY, occupiedCells, random);

        var bossProfile = await _bossProfileResolver.ResolveAsync(roomType, cancellationToken);

        return Room.Create(
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
            template.Version,
            elevation,
            obstacles);
    }

    /// <summary>
    /// Cone-falloff heightmap: a handful of random "peaks" each raise every cell within their
    /// footprint, clamped so adjacent cells never differ by more than 1 (mirrors the frontend's
    /// former cosmetic-only algorithm — this is now the authoritative, server-side version).
    /// </summary>
    private static int[] GenerateElevation(int width, int height, int startX, int startY, Random random)
    {
        var elevation = new int[width * height];
        var peakCount = Math.Max(1, (int)Math.Round((width * height) / 18.0));
        var peaks = new List<(int X, int Y, int Height)>();

        for (var i = 0; i < peakCount; i++)
        {
            peaks.Add((random.Next(width), random.Next(height), 1 + random.Next(RoomGrid.MaxElevation)));
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var cellHeight = 0;

                foreach (var peak in peaks)
                {
                    var distance = Math.Abs(x - peak.X) + Math.Abs(y - peak.Y);
                    cellHeight = Math.Max(cellHeight, peak.Height - distance);
                }

                elevation[(y * width) + x] = Math.Clamp(cellHeight, 0, RoomGrid.MaxElevation);
            }
        }

        // The party always spawns on flat ground.
        elevation[(startY * width) + startX] = 0;

        return elevation;
    }

    /// <summary>
    /// Places obstacles on a random subset of free cells, verifying after every tentative
    /// placement (via BFS from the start cell) that the start, every node, and the boss all
    /// remain mutually reachable — a candidate that would seal off anything is discarded rather
    /// than kept. This guarantees, structurally, that no generated room can maze off a node or
    /// the boss, so <see cref="Room.Create"/>'s own reachability guard never needs to reject a
    /// generated layout (only a hand-built one in a test, say).
    /// </summary>
    private static HashSet<(int X, int Y)> GenerateObstacles(
        int width,
        int height,
        int startX,
        int startY,
        IReadOnlyCollection<(int X, int Y)> occupiedCells,
        Random random)
    {
        var occupiedSet = new HashSet<(int X, int Y)>(occupiedCells);
        var freeCells = new List<(int X, int Y)>();

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (!occupiedSet.Contains((x, y)))
                {
                    freeCells.Add((x, y));
                }
            }
        }

        var candidates = freeCells.OrderBy(_ => random.Next()).ToList();
        var targetCount = (int)(freeCells.Count * ObstacleDensity);
        var obstacles = new HashSet<(int X, int Y)>();

        foreach (var candidate in candidates)
        {
            if (obstacles.Count >= targetCount)
            {
                break;
            }

            obstacles.Add(candidate);

            if (!AllCellsReachable(width, height, startX, startY, occupiedSet, obstacles))
            {
                obstacles.Remove(candidate);
            }
        }

        return obstacles;
    }

    private static bool AllCellsReachable(
        int width,
        int height,
        int startX,
        int startY,
        IReadOnlyCollection<(int X, int Y)> targets,
        IReadOnlyCollection<(int X, int Y)> obstacles)
    {
        var visited = new HashSet<(int X, int Y)> { (startX, startY) };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((startX, startY));

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();

            foreach (var (dx, dy) in Neighbors)
            {
                var neighbor = (X: x + dx, Y: y + dy);

                if (neighbor.X < 0 || neighbor.X >= width || neighbor.Y < 0 || neighbor.Y >= height)
                {
                    continue;
                }

                if (obstacles.Contains(neighbor) || visited.Contains(neighbor))
                {
                    continue;
                }

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return targets.All(visited.Contains);
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
