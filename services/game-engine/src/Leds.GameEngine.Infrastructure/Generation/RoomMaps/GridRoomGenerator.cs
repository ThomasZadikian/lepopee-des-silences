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

    // BALANCE KNOB — fraction of the bounding rectangle carved away so a room stops being a
    // plain rectangle: bites taken out of the edges give L-shapes, alcoves and ragged borders.
    private const double FloorCarveDensity = 0.12;

    // BALANCE KNOB — largest single bite. Small blobs read as architecture; large ones would
    // just shrink the board.
    private const int MaxCarveBlobSize = 3;

    // BALANCE KNOB — how many hidden caches a room tries to place, one per dead-end it can
    // find. A room with no dead end simply gets none.
    private const int MaxHiddenNodes = 2;

    // BALANCE KNOB — movement budget kept ON TOP of the cheapest route to the boss, so reaching
    // the objective is never the only thing the budget affords. Roughly one detour into a recess
    // plus a search or two (see Room.SearchCost).
    private const int MovementBudgetSlack = 12;

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

        var (width, height, startX, startY) = (template.Width, template.Height, template.StartX, template.StartY);

        // Order matters here: the room's shape and its walls are decided BEFORE anything is
        // placed on it, so that recesses and dead ends genuinely exist and hidden caches can be
        // tucked into them — rather than nodes being scattered first and the terrain having to
        // work around them.
        var floor = GenerateFloorMask(width, height, startX, startY, random);
        var elevation = GenerateElevation(width, height, startX, startY, floor, random);
        var (bossX, bossY) = FindFarthestFloorCell(startX, startY, width, height, floor);

        var obstacles = GenerateObstacles(
            width, height, startX, startY, floor, new[] { (bossX, bossY) }, random);

        var deadEnds = FindDeadEnds(width, height, startX, startY, floor, obstacles, (bossX, bossY));

        var nodes = CreateNodes(
            template, profile, floor, obstacles, deadEnds, (bossX, bossY), random);

        var movementBudget = ResolveMovementBudget(
            template, width, height, startX, startY, nodes, elevation, obstacles, floor, bossX, bossY);

        var bossProfile = await _bossProfileResolver.ResolveAsync(roomType, cancellationToken);

        return Room.Create(
            roomDepth,
            roomType,
            palaceState,
            _themeResolver.Resolve(roomType),
            bossProfile,
            nodes,
            width,
            height,
            movementBudget,
            startX,
            startY,
            template.Key,
            template.Version,
            elevation,
            obstacles,
            floor);
    }

    /// <summary>
    /// The template's budget is a floor, not the answer: it was tuned for a plain rectangle and
    /// says nothing about the room actually generated. What matters is that reaching the boss is
    /// never all the budget affords — so the real cheapest route is measured with the real
    /// pathfinder (no duplicated cost formula) and <see cref="MovementBudgetSlack"/> is kept on
    /// top for detours and searching.
    /// </summary>
    private static int ResolveMovementBudget(
        GridRoomLayoutTemplate template,
        int width,
        int height,
        int startX,
        int startY,
        IReadOnlyCollection<MapNode> nodes,
        IReadOnlyList<int> elevation,
        IReadOnlyCollection<(int X, int Y)> obstacles,
        IReadOnlyList<bool> floor,
        int bossX,
        int bossY)
    {
        var probe = RoomGrid.CreateInitial(
            width, height, movementBudget: 0, startX, startY, nodes, elevation, obstacles, floor);

        var bossRoute = probe.FindPath(bossX, bossY);

        // Unreachable should be impossible (obstacle placement is connectivity-checked), but
        // falling back to the template's budget beats generating a room nobody can finish.
        if (bossRoute is null)
        {
            return template.MovementBudget;
        }

        return Math.Max(template.MovementBudget, bossRoute.Value.Cost + MovementBudgetSlack);
    }

    /// <summary>
    /// Carves bites out of the bounding rectangle's edges so a room reads as a place with a
    /// shape — alcoves, an L, a ragged border — instead of a perfect rectangle every time.
    /// Each bite is a small blob grown inward from an edge cell; a bite that would strand part
    /// of the floor is reverted, so the remaining floor is always one connected region.
    /// </summary>
    private static bool[] GenerateFloorMask(int width, int height, int startX, int startY, Random random)
    {
        var floor = Enumerable.Repeat(true, width * height).ToArray();
        var target = (int)(width * height * FloorCarveDensity);

        var edgeCells = new List<(int X, int Y)>();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var isEdge = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                if (isEdge && !(x == startX && y == startY))
                {
                    edgeCells.Add((x, y));
                }
            }
        }

        var carved = 0;

        foreach (var seedCell in edgeCells.OrderBy(_ => random.Next()))
        {
            if (carved >= target)
            {
                break;
            }

            var blob = GrowCarveBlob(seedCell, width, height, startX, startY, floor, random);

            if (blob.Count == 0)
            {
                continue;
            }

            foreach (var (x, y) in blob)
            {
                floor[(y * width) + x] = false;
            }

            if (AllFloorConnected(width, height, startX, startY, floor))
            {
                carved += blob.Count;
            }
            else
            {
                foreach (var (x, y) in blob)
                {
                    floor[(y * width) + x] = true;
                }
            }
        }

        return floor;
    }

    /// <summary>A small connected clump of still-floor cells grown from <paramref name="seedCell"/>.</summary>
    private static List<(int X, int Y)> GrowCarveBlob(
        (int X, int Y) seedCell,
        int width,
        int height,
        int startX,
        int startY,
        bool[] floor,
        Random random)
    {
        var blob = new List<(int X, int Y)>();

        if (!floor[(seedCell.Y * width) + seedCell.X])
        {
            return blob;
        }

        blob.Add(seedCell);
        var size = 1 + random.Next(MaxCarveBlobSize);

        while (blob.Count < size)
        {
            var (fromX, fromY) = blob[random.Next(blob.Count)];
            var (dx, dy) = Neighbors[random.Next(Neighbors.Length)];
            var next = (X: fromX + dx, Y: fromY + dy);

            if (next.X < 0 || next.X >= width || next.Y < 0 || next.Y >= height)
            {
                break;
            }

            // Never carve the party's spawn, and never re-carve a hole.
            if ((next.X == startX && next.Y == startY) || !floor[(next.Y * width) + next.X] || blob.Contains(next))
            {
                break;
            }

            blob.Add(next);
        }

        return blob;
    }

    /// <summary>Every floor cell reachable from the start, ignoring obstacles (shape only).</summary>
    private static bool AllFloorConnected(int width, int height, int startX, int startY, bool[] floor)
    {
        var expected = floor.Count(cell => cell);
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

                if (!floor[(neighbor.Y * width) + neighbor.X] || visited.Contains(neighbor))
                {
                    continue;
                }

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return visited.Count == expected;
    }

    /// <summary>
    /// Walkable cells with exactly one walkable neighbour — the bottom of a recess. These are
    /// where a hidden cache is worth putting: somewhere the player has to deliberately detour to
    /// and would never cross by accident on the way to the boss.
    /// </summary>
    private static List<(int X, int Y)> FindDeadEnds(
        int width,
        int height,
        int startX,
        int startY,
        bool[] floor,
        IReadOnlyCollection<(int X, int Y)> obstacles,
        (int X, int Y) bossCell)
    {
        var obstacleSet = new HashSet<(int X, int Y)>(obstacles);
        var deadEnds = new List<(int X, int Y)>();

        bool Walkable(int x, int y) =>
            x >= 0 && x < width && y >= 0 && y < height
            && floor[(y * width) + x] && !obstacleSet.Contains((x, y));

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!Walkable(x, y) || (x == startX && y == startY) || (x == bossCell.X && y == bossCell.Y))
                {
                    continue;
                }

                var exits = Neighbors.Count(n => Walkable(x + n.Dx, y + n.Dy));

                if (exits == 1)
                {
                    deadEnds.Add((x, y));
                }
            }
        }

        return deadEnds;
    }

    /// <summary>
    /// Cone-falloff heightmap: a handful of random "peaks" each raise every cell within their
    /// footprint, clamped so adjacent cells never differ by more than 1 (mirrors the frontend's
    /// former cosmetic-only algorithm — this is now the authoritative, server-side version).
    /// </summary>
    private static int[] GenerateElevation(
        int width, int height, int startX, int startY, bool[] floor, Random random)
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

                // Holes keep their cone value rather than being flattened: nothing reads the
                // elevation of a non-floor cell (line of sight skips them, the renderer draws
                // no tile there), and zeroing them would break the 1-Lipschitz property the
                // heightmap is built to guarantee — a hole beside a peak would read as a 2-step
                // cliff in the raw array.
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
        bool[] floor,
        IReadOnlyCollection<(int X, int Y)> protectedCells,
        Random random)
    {
        var protectedSet = new HashSet<(int X, int Y)>(protectedCells) { (startX, startY) };
        var freeCells = new List<(int X, int Y)>();

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (floor[(y * width) + x] && !protectedSet.Contains((x, y)))
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

            // Every remaining walkable cell must stay reachable — not merely the start and the
            // boss. Nodes are placed after this pass, so anywhere they could land is guaranteed
            // reachable by construction, and no walkable island can ever appear.
            if (!AllWalkableReachable(width, height, startX, startY, floor, obstacles))
            {
                obstacles.Remove(candidate);
            }
        }

        return obstacles;
    }

    private static bool AllWalkableReachable(
        int width,
        int height,
        int startX,
        int startY,
        bool[] floor,
        IReadOnlyCollection<(int X, int Y)> obstacles)
    {
        var obstacleSet = new HashSet<(int X, int Y)>(obstacles);

        var expected = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (floor[(y * width) + x] && !obstacleSet.Contains((x, y)))
                {
                    expected++;
                }
            }
        }

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

                if (!floor[(neighbor.Y * width) + neighbor.X]
                    || obstacleSet.Contains(neighbor)
                    || visited.Contains(neighbor))
                {
                    continue;
                }

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return visited.Count == expected;
    }

    /// <summary>
    /// ⚠ AUTHORED TABLE, PENDING REVIEW — what walking onto a node's cell does, per event type.
    /// The creator's steer was "combat and NPCs" trigger on contact; the rest below is a
    /// defensible first pass, not a settled design:
    /// <list type="bullet">
    /// <item>Combat/Rare/Curse trigger on contact but can be walked around — a fight or a curse
    /// does not wait for you to accept it, yet you can still choose your route.</item>
    /// <item>Elite blocks: a strong guardian standing in a corridor is the one thing that should
    /// genuinely bar the way.</item>
    /// <item>Npc triggers on contact — someone who wants to talk to you does not wait to be
    /// clicked.</item>
    /// <item>Bosses stay optional: they already have their own entry path, and the remote
    /// challenge exists precisely so a boss is never a wall.</item>
    /// <item>Item/Memory/Rest/Merchant/Law stay optional — nothing about them justifies forcing
    /// the player's hand.</item>
    /// </list>
    /// </summary>
    private static ContactBehavior ResolveContactBehavior(NodeEventType type) => type switch
    {
        NodeEventType.Elite => ContactBehavior.Blocking,
        NodeEventType.Combat or NodeEventType.Rare or NodeEventType.Curse or NodeEventType.Npc
            => ContactBehavior.TriggerOnEnter,
        _ => ContactBehavior.None,
    };

    /// <summary>
    /// ⚠ AUTHORED TABLE, PENDING REVIEW — the warning a contact node gives off, per the creator's
    /// "it depends on the node type". A visible guardian or a person is readable from a distance;
    /// an ordinary ambush is a coin flip between a tell and nothing at all. <c>None</c> on a
    /// contact node IS the ambush, and is meant to stay indistinguishable from plain floor.
    /// </summary>
    private static DangerTell ResolveDangerTell(NodeEventType type, Random random) => type switch
    {
        NodeEventType.Elite => DangerTell.Tracks,
        NodeEventType.Npc => DangerTell.Glow,
        NodeEventType.Rare => DangerTell.Glow,
        NodeEventType.Curse => DangerTell.Blight,
        NodeEventType.Combat => random.Next(2) == 0 ? DangerTell.Tracks : DangerTell.None,
        _ => DangerTell.None,
    };

    private static List<MapNode> CreateNodes(
        GridRoomLayoutTemplate template,
        RoomTypeGenerationProfile profile,
        bool[] floor,
        IReadOnlyCollection<(int X, int Y)> obstacles,
        IReadOnlyList<(int X, int Y)> deadEnds,
        (int X, int Y) bossCell,
        Random random)
    {
        var obstacleSet = new HashSet<(int X, int Y)>(obstacles);
        var occupiedCells = new HashSet<(int X, int Y)>
        {
            (template.StartX, template.StartY),
            bossCell,
        };

        var nodeCount = random.Next(template.MinNodeCount, template.MaxNodeCount + 1);
        var nodes = new List<MapNode>();

        // Hidden caches first, so they get the dead ends before ordinary nodes can take them.
        var hiddenCells = deadEnds
            .Where(cell => !occupiedCells.Contains(cell))
            .OrderBy(_ => random.Next())
            .Take(MaxHiddenNodes)
            .ToList();

        foreach (var (x, y) in hiddenCells)
        {
            occupiedCells.Add((x, y));

            // A cache is loot, and loot only: the reward for detouring should never turn out to
            // be a fight the player did not choose.
            nodes.Add(MapNode.Create(
                eventType: NodeEventType.Item,
                riskLevel: random.Next(profile.RiskMin, profile.RiskMax),
                rewardProfile: NodeGenerationHeuristics.PickRewardProfile(NodeEventType.Item, profile, random),
                row: y,
                lane: x,
                parentNodeIds: Array.Empty<NodeId>(),
                isBoss: false,
                initialState: NodeState.Available,
                combatRiskTier: null,
                hiddenState: HiddenState.Hint));
        }

        var remainingCount = Math.Max(0, nodeCount - 1 - nodes.Count);

        var freeCells = new List<(int X, int Y)>();
        for (var x = 0; x < template.Width; x++)
        {
            for (var y = 0; y < template.Height; y++)
            {
                if (floor[(y * template.Width) + x]
                    && !obstacleSet.Contains((x, y))
                    && !occupiedCells.Contains((x, y)))
                {
                    freeCells.Add((x, y));
                }
            }
        }

        var chosenCells = freeCells
            .OrderBy(_ => random.Next())
            .Take(Math.Min(remainingCount, freeCells.Count))
            .ToList();

        foreach (var (x, y) in chosenCells)
        {
            var type = NodeGenerationHeuristics.PickWeightedNodeType(profile, random);
            var riskLevel = random.Next(profile.RiskMin, profile.RiskMax);
            var combatRiskTier = NodeGenerationHeuristics.DeriveCombatRiskTier(type, riskLevel);
            var rewardProfile = NodeGenerationHeuristics.PickRewardProfile(type, profile, random);
            var contactBehavior = ResolveContactBehavior(type);
            var dangerTell = contactBehavior == ContactBehavior.None
                ? DangerTell.None
                : ResolveDangerTell(type, random);

            nodes.Add(MapNode.Create(
                eventType: type,
                riskLevel,
                rewardProfile,
                row: y,
                lane: x,
                parentNodeIds: Array.Empty<NodeId>(),
                isBoss: false,
                initialState: NodeState.Available,
                combatRiskTier: combatRiskTier,
                dangerTell: dangerTell,
                contactBehavior: contactBehavior));
        }

        nodes.Add(MapNode.Create(
            eventType: NodeEventType.RoomBoss,
            riskLevel: 85,
            rewardProfile: "room-boss",
            row: bossCell.Y,
            lane: bossCell.X,
            parentNodeIds: Array.Empty<NodeId>(),
            isBoss: true,
            initialState: NodeState.Available,
            combatRiskTier: NodeGenerationHeuristics.DeriveCombatRiskTier(NodeEventType.RoomBoss, 85)));

        return nodes;
    }

    /// <summary>
    /// The boss goes as far from the spawn as the room's actual shape allows. Deterministic
    /// given (start, size, floor) — does not consume the seeded <see cref="Random"/>, so the
    /// boss's position depends on the room's shape rather than on its roll sequence. Ties broken
    /// by (X desc, Y desc).
    /// </summary>
    private static (int X, int Y) FindFarthestFloorCell(
        int startX, int startY, int width, int height, bool[] floor)
    {
        var best = (X: startX, Y: startY);
        var bestDistance = -1;

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (!floor[(y * width) + x])
                {
                    continue;
                }

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
