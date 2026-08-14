using Leds.GameEngine.Application.Protocol;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Protocol;
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
    // find. A room with no dead end simply gets none. Raised alongside the room-size increase
    // (exploration-camera plan, Workstream B) — a bigger board has more dead-end pockets worth
    // finding.
    private const int MaxHiddenNodes = 4;

    // BALANCE KNOB — movement budget kept ON TOP of the cheapest route to the boss, so reaching
    // the objective is never the only thing the budget affords. Roughly one detour into a recess
    // plus a search or two (see Room.SearchCost).
    private const int MovementBudgetSlack = 12;

    // BALANCE KNOB — sub-room count/size, for the Rectangular carving style when
    // RoomStructuralProfile.SubRoomsAllowed. Kept small and modest: this is a first pass at
    // "chambers behind a door," not full architectural layout.
    private const int MaxSubRoomAttempts = 6;
    private const int MaxSubRoomCount = 3;
    private const int MinSubRoomSize = 4;
    private const int MaxSubRoomSize = 6;

    private static readonly (int Dx, int Dy)[] Neighbors = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    private readonly IGridRoomLayoutTemplateProvider _templateProvider;
    private readonly IRoomThemeResolver _themeResolver;
    private readonly IRoomBossProfileResolver _bossProfileResolver;
    private readonly IRoomTypeGenerationProfileProvider _generationProfileProvider;
    private readonly IRoomStructuralProfileProvider _structuralProfileProvider;
    private readonly ILocalRuleProvider _localRuleProvider;

    public GridRoomGenerator(
        IGridRoomLayoutTemplateProvider templateProvider,
        IRoomThemeResolver themeResolver,
        IRoomBossProfileResolver bossProfileResolver,
        IRoomTypeGenerationProfileProvider generationProfileProvider,
        IRoomStructuralProfileProvider structuralProfileProvider,
        ILocalRuleProvider localRuleProvider)
    {
        _templateProvider = templateProvider;
        _themeResolver = themeResolver;
        _bossProfileResolver = bossProfileResolver;
        _generationProfileProvider = generationProfileProvider;
        _structuralProfileProvider = structuralProfileProvider;
        _localRuleProvider = localRuleProvider;
    }

    /// <summary>Attaches run-scoped <see cref="LocalRuleState"/> tracking for every authored
    /// <see cref="LocalRule"/> this room's catalog key carries — generic across any room, not a
    /// Hall-only step (SFD Hall d'entrée §IX: the protocol engine "ne crée pas un moteur
    /// spécifique au Hall").</summary>
    private void AttachLocalRuleStates(Room room, string? catalogRoomKey)
    {
        if (catalogRoomKey is null)
        {
            return;
        }

        foreach (var rule in _localRuleProvider.GetRulesForRoom(catalogRoomKey))
        {
            room.AddLocalRuleState(LocalRuleState.Create(rule.Key));
        }
    }

    public async Task<Room> GenerateAsync(
        string seed,
        string generatorVersion,
        int roomDepth,
        RoomType roomType,
        Random random,
        CancellationToken cancellationToken = default,
        PalaceRoomState palaceState = PalaceRoomState.Neutral,
        string? catalogRoomKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seed);
        ArgumentException.ThrowIfNullOrWhiteSpace(generatorVersion);
        ArgumentNullException.ThrowIfNull(random);

        var template = _templateProvider.GetTemplate(roomType, generatorVersion, catalogRoomKey);

        if (string.Equals(catalogRoomKey, "room.halldentree", StringComparison.OrdinalIgnoreCase))
        {
            return await GenerateHallEntreeAsync(roomDepth, roomType, palaceState, template, cancellationToken);
        }

        var profile = _generationProfileProvider.GetProfile(roomType);
        var structuralProfile = _structuralProfileProvider.GetProfile(roomType, catalogRoomKey);

        var (width, height, startX, startY) = (template.Width, template.Height, template.StartX, template.StartY);

        // Order matters here: the room's shape and its walls are decided BEFORE anything is
        // placed on it, so that recesses and dead ends genuinely exist and hidden caches can be
        // tucked into them — rather than nodes being scattered first and the terrain having to
        // work around them.
        var floor = GenerateFloorMask(width, height, startX, startY, structuralProfile.CarvingStyle, random);
        var (bossX, bossY) = FindFarthestFloorCell(startX, startY, width, height, floor);

        IReadOnlyList<(int X, int Y)> doors = [];
        if (structuralProfile.SubRoomsAllowed)
        {
            (floor, doors) = PartitionSubRooms(
                width, height, startX, startY, (bossX, bossY), floor, structuralProfile.SubRoomsAllowed, random);
        }

        var elevation = GenerateElevation(width, height, startX, startY, floor, random);

        var obstacles = GenerateObstacles(
            width, height, startX, startY, floor, new[] { (bossX, bossY) }, random);

        var deadEnds = FindDeadEnds(width, height, startX, startY, floor, obstacles, (bossX, bossY));

        var nodes = CreateNodes(
            template, profile, floor, obstacles, deadEnds, (bossX, bossY), random);

        var movementBudget = ResolveMovementBudget(
            template, width, height, startX, startY, nodes, elevation, obstacles, floor, bossX, bossY);

        var bossProfile = await _bossProfileResolver.ResolveAsync(roomType, cancellationToken);

        var room = Room.Create(
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
            floor,
            doors);

        AttachLocalRuleStates(room, catalogRoomKey);

        return room;
    }

    /// <summary>
    /// The Hall d'entrée's dedicated path — SFD Hall d'entrée §III/§VIII/§IX: "squelette authored
    /// fixe", not the random carve every other room gets. Reuses everything downstream of shape
    /// (movement budget resolution, boss profile lookup, <see cref="Room.Create"/> and its
    /// invariants) unchanged; only floor/elevation/obstacles/doors/nodes are authored instead of
    /// rolled. Population (see <see cref="Hall.HallEntreeCasting"/>) is added once the room
    /// exists, mirroring how <see cref="DeterministicRunGenerator"/> attaches exits afterward
    /// rather than threading them through <see cref="Room.Create"/> itself.
    /// </summary>
    private async Task<Room> GenerateHallEntreeAsync(
        int roomDepth,
        RoomType roomType,
        PalaceRoomState palaceState,
        GridRoomLayoutTemplate template,
        CancellationToken cancellationToken)
    {
        var layout = Hall.HallEntreeLayout.Build();

        var bossNode = MapNode.Create(
            eventType: NodeEventType.RoomBoss,
            riskLevel: 85,
            rewardProfile: "room-boss",
            row: Hall.HallEntreeLayout.BossY,
            lane: Hall.HallEntreeLayout.BossX,
            parentNodeIds: Array.Empty<NodeId>(),
            isBoss: true,
            initialState: NodeState.Available,
            combatRiskTier: NodeGenerationHeuristics.DeriveCombatRiskTier(NodeEventType.RoomBoss, 85));

        var nodes = new List<MapNode> { bossNode };

        // Run.StartNew requires a run's opening room to hold 6-30 content nodes — the Hall must
        // clear that floor to ever actually open a run, not just to pass this test in isolation.
        // See Hall.HallEntreeLayout.CurioCells's own remarks for why these are placeholders.
        foreach (var (x, y) in Hall.HallEntreeLayout.CurioCells)
        {
            nodes.Add(MapNode.Create(
                eventType: NodeEventType.Item,
                riskLevel: 0,
                rewardProfile: "standard",
                row: y,
                lane: x,
                parentNodeIds: Array.Empty<NodeId>(),
                isBoss: false,
                initialState: NodeState.Available));
        }

        var movementBudget = ResolveMovementBudget(
            template,
            Hall.HallEntreeLayout.Width,
            Hall.HallEntreeLayout.Height,
            Hall.HallEntreeLayout.StartX,
            Hall.HallEntreeLayout.StartY,
            nodes,
            layout.Elevation,
            layout.Obstacles,
            layout.Floor,
            Hall.HallEntreeLayout.BossX,
            Hall.HallEntreeLayout.BossY);

        var bossProfile = await _bossProfileResolver.ResolveAsync(roomType, cancellationToken);

        var room = Room.Create(
            roomDepth,
            roomType,
            palaceState,
            _themeResolver.Resolve(roomType),
            bossProfile,
            nodes,
            Hall.HallEntreeLayout.Width,
            Hall.HallEntreeLayout.Height,
            movementBudget,
            Hall.HallEntreeLayout.StartX,
            Hall.HallEntreeLayout.StartY,
            template.Key,
            template.Version,
            layout.Elevation,
            layout.Obstacles,
            layout.Floor,
            layout.Doors);

        foreach (var entry in Hall.HallEntreeCasting.Roster)
        {
            room.AddRoomNpc(RoomNpc.Create(
                entry.CatalogNpcKey, entry.X, entry.Y, entry.Behavior, entry.AwarenessRadius));
        }

        AttachLocalRuleStates(room, "room.halldentree");

        return room;
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
    /// <summary>
    /// Cells on the bounding rectangle's border, excluding the party's starting cell. Pure
    /// function of the room's size/start — no RNG consumed. Exposed publicly as the natural
    /// candidate pool for exit placement (see DeterministicRunGenerator's post-generation
    /// exit-attachment step): a room's exits are meant to sit at its edge, not in the middle.
    /// </summary>
    public static IReadOnlyList<(int X, int Y)> ComputeEdgeCells(int width, int height, int startX, int startY)
    {
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

        return edgeCells;
    }

    /// <summary>
    /// Dispatches to the room's silhouette-carving algorithm per <see cref="CarvingStyle"/> —
    /// see <see cref="RoomStructuralProfile"/>. <see cref="CarvingStyle.Rectangular"/> is
    /// today's only algorithm (edge bites); <see cref="CarvingStyle.Organic"/> is new, for
    /// rooms like a jardin or a crystal caverne where the terrain itself — not a built enceinte
    /// — should define the shape.
    /// </summary>
    private static bool[] GenerateFloorMask(
        int width, int height, int startX, int startY, CarvingStyle carvingStyle, Random random)
    {
        return carvingStyle == CarvingStyle.Organic
            ? GenerateOrganicFloorMask(width, height, startX, startY, random)
            : GenerateRectangularFloorMask(width, height, startX, startY, random);
    }

    private static bool[] GenerateRectangularFloorMask(int width, int height, int startX, int startY, Random random)
    {
        var floor = Enumerable.Repeat(true, width * height).ToArray();
        var target = (int)(width * height * FloorCarveDensity);
        var edgeCells = ComputeEdgeCells(width, height, startX, startY);

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

    /// <summary>
    /// Carves a wavering disc around the room's center — formula straight from the Claude
    /// Design "salle" handoff's README (§ "Cohérence des sous-pièces"): a radius that ripples
    /// with the angle around the center, instead of a rectangle with bites taken out. Never
    /// produces an enclosed interior chamber — the contour IS the room, by construction, which
    /// is exactly why <see cref="RoomStructuralProfile.SubRoomsAllowed"/> is always false for
    /// this style.
    /// </summary>
    private static bool[] GenerateOrganicFloorMask(int width, int height, int startX, int startY, Random random)
    {
        var floor = new bool[width * height];
        var centerX = (width - 1) / 2.0;
        var centerY = (height - 1) / 2.0;
        var maxRadius = Math.Min(width, height) / 2.0;
        var phase = random.NextDouble() * Math.PI * 2;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var nx = (x - centerX) / maxRadius;
                var ny = (y - centerY) / maxRadius;
                var distance = Math.Sqrt((nx * nx) + (ny * ny));
                var angle = Math.Atan2(ny, nx);
                var threshold = 0.9 + (0.22 * Math.Sin((3 * angle) + phase));

                floor[(y * width) + x] = distance < threshold;
            }
        }

        // The party's spawn sits wherever the template says (an edge cell, by the same
        // convention every template uses) — almost certainly outside the organic disc, which is
        // centered. Force it to floor and stitch it to the disc with a straight corridor rather
        // than assume every template's start position happens to land inside the silhouette.
        floor[(startY * width) + startX] = true;
        ConnectCellToFloor(floor, width, height, startX, startY);

        return floor;
    }

    /// <summary>
    /// If <paramref name="cellX"/>/<paramref name="cellY"/> isn't already connected to the rest
    /// of the floor, carves a straight L-shaped corridor (horizontal then vertical) from it to
    /// the nearest other floor cell, forcing every cell along the way to floor. A single such
    /// corridor is enough to stitch a start cell to a convex-ish blob like the organic carve
    /// produces.
    /// </summary>
    private static void ConnectCellToFloor(bool[] floor, int width, int height, int cellX, int cellY)
    {
        if (AllFloorConnected(width, height, cellX, cellY, floor))
        {
            return;
        }

        var nearest = (X: -1, Y: -1);
        var nearestDistance = int.MaxValue;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!floor[(y * width) + x] || (x == cellX && y == cellY))
                {
                    continue;
                }

                var distance = Math.Abs(x - cellX) + Math.Abs(y - cellY);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = (x, y);
                }
            }
        }

        if (nearest.X < 0)
        {
            // Nothing else is floor at all — the disc missed entirely (a vanishingly small
            // template would have to produce this). Leave just the spawn cell as floor; the
            // caller's own downstream checks (Room.Create's node-reachability guard) would
            // reject a room this degenerate before it ever reaches a player.
            return;
        }

        var stepX = Math.Sign(nearest.X - cellX);
        for (var x = cellX; x != nearest.X; x += stepX)
        {
            floor[(cellY * width) + x] = true;
        }

        var stepY = Math.Sign(nearest.Y - cellY);
        for (var y = cellY; y != nearest.Y; y += stepY)
        {
            floor[(y * width) + nearest.X] = true;
        }

        floor[(nearest.Y * width) + nearest.X] = true;
    }

    /// <summary>
    /// Partitions a handful of small enclosed chambers into an already-carved
    /// <see cref="CarvingStyle.Rectangular"/> silhouette — a "sub-room" is a rectangle whose
    /// border becomes void (so a wall renders around it, per the arête-based wall model) except
    /// for one door cell left as floor. Only ever called when
    /// <see cref="RoomStructuralProfile.SubRoomsAllowed"/> — the flag is threaded through as an
    /// explicit parameter (rather than trusted implicitly from the call site) so a regression
    /// that calls this without checking the profile fails loudly instead of silently
    /// partitioning a room that was never supposed to have interior chambers.
    /// </summary>
    private static (bool[] Floor, IReadOnlyList<(int X, int Y)> Doors) PartitionSubRooms(
        int width,
        int height,
        int startX,
        int startY,
        (int X, int Y) bossCell,
        bool[] floor,
        bool subRoomsAllowed,
        Random random)
    {
        if (!subRoomsAllowed)
        {
            throw new DomainException(
                "PartitionSubRooms must not be called for a room whose structural profile disallows sub-rooms.");
        }

        var placed = 0;
        var doors = new List<(int X, int Y)>();

        for (var attempt = 0; attempt < MaxSubRoomAttempts && placed < MaxSubRoomCount; attempt++)
        {
            var rectWidth = MinSubRoomSize + random.Next(MaxSubRoomSize - MinSubRoomSize + 1);
            var rectHeight = MinSubRoomSize + random.Next(MaxSubRoomSize - MinSubRoomSize + 1);

            // Kept strictly interior (never touching the outer boundary) so a sub-room's ring
            // reads as its own partition, not a redundant trace over the room's own outer wall.
            var maxRectX = width - rectWidth - 1;
            var maxRectY = height - rectHeight - 1;

            if (maxRectX < 1 || maxRectY < 1)
            {
                continue;
            }

            var rectX = 1 + random.Next(maxRectX);
            var rectY = 1 + random.Next(maxRectY);

            if (!IsRectangleFullyFloor(floor, width, rectX, rectY, rectWidth, rectHeight)
                || RectangleContains(rectX, rectY, rectWidth, rectHeight, startX, startY)
                || RectangleContains(rectX, rectY, rectWidth, rectHeight, bossCell.X, bossCell.Y))
            {
                continue;
            }

            var ring = ComputeRectangleRing(rectX, rectY, rectWidth, rectHeight);
            var doorCell = ring[random.Next(ring.Count)];
            var reverted = new List<(int X, int Y)>();

            foreach (var cell in ring)
            {
                if (cell == doorCell)
                {
                    continue;
                }

                floor[(cell.Y * width) + cell.X] = false;
                reverted.Add(cell);
            }

            if (AllFloorConnected(width, height, startX, startY, floor))
            {
                placed++;
                doors.Add(doorCell);
            }
            else
            {
                foreach (var (x, y) in reverted)
                {
                    floor[(y * width) + x] = true;
                }
            }
        }

        return (floor, doors);
    }

    private static bool IsRectangleFullyFloor(bool[] floor, int width, int rectX, int rectY, int rectWidth, int rectHeight)
    {
        for (var y = rectY; y < rectY + rectHeight; y++)
        {
            for (var x = rectX; x < rectX + rectWidth; x++)
            {
                if (!floor[(y * width) + x])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool RectangleContains(int rectX, int rectY, int rectWidth, int rectHeight, int x, int y)
    {
        return x >= rectX && x < rectX + rectWidth && y >= rectY && y < rectY + rectHeight;
    }

    /// <summary>The border cells of a rectangle — what becomes void to wall the sub-room off.</summary>
    private static List<(int X, int Y)> ComputeRectangleRing(int rectX, int rectY, int rectWidth, int rectHeight)
    {
        var ring = new List<(int X, int Y)>();

        for (var y = rectY; y < rectY + rectHeight; y++)
        {
            for (var x = rectX; x < rectX + rectWidth; x++)
            {
                var isRing = x == rectX || x == rectX + rectWidth - 1 || y == rectY || y == rectY + rectHeight - 1;
                if (isRing)
                {
                    ring.Add((x, y));
                }
            }
        }

        return ring;
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
        var peakCount = Math.Max(1, (int)Math.Round((width * height) / 30.0));
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

        // One guaranteed breather per room. Left to the weighted draw, Rest is only one type
        // among a dozen and a room can easily come out with none at all — which makes the run's
        // attrition a matter of luck rather than of the routing choices the player made. It goes
        // on the cell furthest from the entrance so it is a place you reach, not one you trip
        // over on the first step. Nothing stops the weighted draw below from rolling more.
        var guaranteedRestCell = chosenCells.Count == 0
            ? (Cell: default((int X, int Y)), Exists: false)
            : (Cell: chosenCells
                .OrderByDescending(c => Math.Abs(c.X - template.StartX) + Math.Abs(c.Y - template.StartY))
                .First(), Exists: true);

        foreach (var (x, y) in chosenCells)
        {
            var type = guaranteedRestCell.Exists && (x, y) == guaranteedRestCell.Cell
                ? NodeEventType.Rest
                : NodeGenerationHeuristics.PickWeightedNodeType(profile, random);
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
