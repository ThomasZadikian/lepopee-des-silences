using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Domain.Rooms;

/// <summary>
/// Free-movement grid overlay for a room. Owned by <see cref="Room"/> (<see cref="Room.Grid"/>).
/// Movement is orthogonal (4 directions); cost accounts for real obstacles (impassable, routed
/// around via <see cref="FindPath"/>) and elevation (climbing costs extra, descending is free).
/// Fog of war reveals cells/nodes within <see cref="VisionRadius"/> of every cell the party has
/// stood on or passed through, gated by line of sight (obstacles and tall terrain can block it).
/// </summary>
public sealed class RoomGrid
{
    // BALANCE KNOB — how many cells around the party are revealed by fog of war. Not
    // configurable per-room in v1.
    public const int VisionRadius = 2;

    // BALANCE KNOB — elevation levels are 0 (flat ground) through this value.
    public const int MaxElevation = 3;

    private static readonly (int Dx, int Dy)[] OrthogonalNeighbors =
    {
        (1, 0), (-1, 0), (0, 1), (0, -1),
    };

    private readonly int[] _elevation;
    private readonly bool[] _isFloor;
    private readonly HashSet<(int X, int Y)> _obstacles;
    private readonly HashSet<NodeId> _revealedNodeIds;
    private readonly HashSet<(int X, int Y)> _revealedCells;

    private RoomGrid(
        int width,
        int height,
        int movementBudget,
        int movementBudgetRemaining,
        int startX,
        int startY,
        int partyX,
        int partyY,
        HashSet<NodeId> revealedNodeIds,
        HashSet<(int X, int Y)> revealedCells,
        int[] elevation,
        HashSet<(int X, int Y)> obstacles,
        bool[] isFloor)
    {
        _isFloor = isFloor;
        Width = width;
        Height = height;
        MovementBudget = movementBudget;
        MovementBudgetRemaining = movementBudgetRemaining;
        StartX = startX;
        StartY = startY;
        PartyX = partyX;
        PartyY = partyY;
        _revealedNodeIds = revealedNodeIds;
        _revealedCells = revealedCells;
        _elevation = elevation;
        _obstacles = obstacles;
    }

    public int Width { get; }

    public int Height { get; }

    public int MovementBudget { get; }

    public int MovementBudgetRemaining { get; private set; }

    public int StartX { get; }

    public int StartY { get; }

    public int PartyX { get; private set; }

    public int PartyY { get; private set; }

    public IReadOnlyCollection<NodeId> RevealedNodeIds => _revealedNodeIds;

    public IReadOnlyCollection<(int X, int Y)> RevealedCells => _revealedCells;

    /// <summary>Flat, row-major (index = y * Width + x). Immutable for the room's lifetime.</summary>
    public IReadOnlyList<int> Elevation => _elevation;

    /// <summary>Impassable cells. Immutable for the room's lifetime.</summary>
    public IReadOnlyCollection<(int X, int Y)> Obstacles => _obstacles;

    /// <summary>
    /// Which cells are part of the room at all, flat row-major like <see cref="Elevation"/>.
    /// The grid stays a full rectangle as a coordinate system; a cell that is not floor is a
    /// hole in that rectangle — not walkable, not visible, not paintable — which is what lets a
    /// room be L-shaped, have recesses, or end in a ragged edge. Immutable for the room's
    /// lifetime.
    /// </summary>
    public IReadOnlyList<bool> FloorMask => _isFloor;

    public int ElevationAt(int x, int y) => _elevation[(y * Width) + x];

    public bool IsObstacle(int x, int y) => _obstacles.Contains((x, y));

    /// <summary>
    /// Whether (x, y) is a cell of this room. Returns false out of bounds too, so callers can
    /// use it as a single "is this a real cell" test instead of pairing it with a bounds check.
    /// </summary>
    public bool IsFloor(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return false;
        }

        return _isFloor[(y * Width) + x];
    }

    /// <summary>Floor, and not blocked by an obstacle — the test the pathfinder enqueues on.</summary>
    public bool IsWalkable(int x, int y) => IsFloor(x, y) && !IsObstacle(x, y);

    public static RoomGrid CreateInitial(
        int width,
        int height,
        int movementBudget,
        int startX,
        int startY,
        IReadOnlyCollection<MapNode> nodes,
        IReadOnlyList<int>? elevation = null,
        IReadOnlyCollection<(int X, int Y)>? obstacles = null,
        IReadOnlyList<bool>? floorCells = null)
    {
        if (width <= 0)
        {
            throw new DomainException("Grid width must be greater than 0.");
        }

        if (height <= 0)
        {
            throw new DomainException("Grid height must be greater than 0.");
        }

        if (movementBudget < 0)
        {
            throw new DomainException("Movement budget must be greater than or equal to 0.");
        }

        if (startX < 0 || startX >= width || startY < 0 || startY >= height)
        {
            throw new DomainException("Grid start position must be within the grid bounds.");
        }

        // Default: the whole rectangle is floor — a room with no declared shape is the plain
        // rectangle rooms were before shapes existed.
        var resolvedFloor = floorCells is null
            ? Enumerable.Repeat(true, width * height).ToArray()
            : floorCells.ToArray();

        if (resolvedFloor.Length != width * height)
        {
            throw new DomainException("Floor mask must have exactly Width*Height entries.");
        }

        if (!resolvedFloor[(startY * width) + startX])
        {
            throw new DomainException("The party's starting cell must be part of the room.");
        }

        if (!resolvedFloor.Any(cell => cell))
        {
            throw new DomainException("A room must contain at least one floor cell.");
        }

        var resolvedElevation = elevation is null ? new int[width * height] : elevation.ToArray();

        if (resolvedElevation.Length != width * height)
        {
            throw new DomainException("Elevation map must have exactly Width*Height entries.");
        }

        foreach (var level in resolvedElevation)
        {
            if (level < 0 || level > MaxElevation)
            {
                throw new DomainException($"Elevation values must be between 0 and {MaxElevation}.");
            }
        }

        var resolvedObstacles = obstacles is null
            ? new HashSet<(int X, int Y)>()
            : new HashSet<(int X, int Y)>(obstacles);

        foreach (var (obstacleX, obstacleY) in resolvedObstacles)
        {
            if (obstacleX < 0 || obstacleX >= width || obstacleY < 0 || obstacleY >= height)
            {
                throw new DomainException("Obstacle cells must be within the grid bounds.");
            }
        }

        if (resolvedObstacles.Contains((startX, startY)))
        {
            throw new DomainException("The party's starting cell cannot be an obstacle.");
        }

        // An obstacle on a hole would be doubly impassable and impossible to render coherently —
        // it means the generator disagrees with itself about where the room is.
        foreach (var (obstacleX, obstacleY) in resolvedObstacles)
        {
            if (!resolvedFloor[(obstacleY * width) + obstacleX])
            {
                throw new DomainException("An obstacle cannot sit outside the room's floor.");
            }
        }

        var grid = new RoomGrid(
            width, height, movementBudget, movementBudget,
            startX, startY, startX, startY,
            new HashSet<NodeId>(), new HashSet<(int X, int Y)>(),
            resolvedElevation, resolvedObstacles, resolvedFloor);

        grid.RevealAround(startX, startY, nodes);

        return grid;
    }

    /// <summary>
    /// Finds the cheapest walkable route from the party's current position to
    /// (targetX, targetY), routing around obstacle cells and holes in the room's shape entirely
    /// (never enqueued) and pricing each orthogonal step at 1 plus any elevation gained climbing
    /// into it (descending is free). Returns null when the target is not a walkable cell or is
    /// unreachable. The returned path starts with the first step taken (excludes the party's
    /// current cell) and ends with the target cell.
    /// <para>
    /// <paramref name="transitBlockers"/> are cells a route may END on but never pass THROUGH —
    /// how a blocking node becomes a real lock on a corridor. Pass null to ignore locks entirely,
    /// which is what a "could the party ever get there at all" check wants (see
    /// <c>Room.Create</c>'s reachability guards): a lock is meant to be dealt with, not to make
    /// the room permanently impassable.
    /// </para>
    /// </summary>
    public (IReadOnlyList<(int X, int Y)> Path, int Cost)? FindPath(
        int targetX,
        int targetY,
        IReadOnlySet<(int X, int Y)>? transitBlockers = null)
    {
        // IsWalkable covers bounds, holes in the room's shape, and obstacles in one test.
        if (!IsWalkable(targetX, targetY))
        {
            return null;
        }

        var size = Width * Height;
        var distance = new int[size];
        Array.Fill(distance, int.MaxValue);
        var visited = new bool[size];
        var previous = new int[size];
        Array.Fill(previous, -1);

        var startIndex = IndexOf(PartyX, PartyY);
        distance[startIndex] = 0;

        for (var iteration = 0; iteration < size; iteration++)
        {
            var current = -1;
            var currentDistance = int.MaxValue;

            for (var i = 0; i < size; i++)
            {
                if (!visited[i] && distance[i] < currentDistance)
                {
                    current = i;
                    currentDistance = distance[i];
                }
            }

            if (current == -1)
            {
                break;
            }

            visited[current] = true;

            var currentX = current % Width;
            var currentY = current / Width;

            if (currentX == targetX && currentY == targetY)
            {
                break;
            }

            // A blocking cell can be walked ONTO but not THROUGH: reaching it settles its own
            // distance (so it stays a valid destination), but we never expand past it.
            if (transitBlockers is not null && transitBlockers.Contains((currentX, currentY)))
            {
                continue;
            }

            foreach (var (dx, dy) in OrthogonalNeighbors)
            {
                var neighborX = currentX + dx;
                var neighborY = currentY + dy;

                if (!IsWalkable(neighborX, neighborY))
                {
                    continue;
                }

                var neighborIndex = IndexOf(neighborX, neighborY);

                if (visited[neighborIndex])
                {
                    continue;
                }

                var stepCost = 1 + Math.Max(0, ElevationAt(neighborX, neighborY) - ElevationAt(currentX, currentY));
                var candidateDistance = distance[current] + stepCost;

                if (candidateDistance < distance[neighborIndex])
                {
                    distance[neighborIndex] = candidateDistance;
                    previous[neighborIndex] = current;
                }
            }
        }

        var targetIndex = IndexOf(targetX, targetY);

        if (distance[targetIndex] == int.MaxValue)
        {
            return null;
        }

        var path = new List<(int X, int Y)>();
        var node = targetIndex;

        while (node != startIndex)
        {
            path.Add((node % Width, node / Width));
            node = previous[node];

            if (node == -1)
            {
                return null;
            }
        }

        path.Reverse();

        return (path, distance[targetIndex]);
    }

    /// <summary>
    /// Walks the party along an already-validated <paramref name="path"/> (see
    /// <see cref="FindPath"/> — reachability and budget already checked by
    /// <see cref="Room.MoveParty"/>), revealing fog of war at every cell walked through rather
    /// than only at the destination.
    /// <para>
    /// The walk stops early on the first cell holding a contact-triggered node, and only the
    /// distance actually covered is charged — stepping into an ambush costs what it cost to get
    /// there, not the move the party had intended. Returns that node, or null if the party
    /// walked the whole way; the caller turns a non-null result into the node interaction (see
    /// <see cref="Room.MoveParty"/>).
    /// </para>
    /// </summary>
    public MapNode? MoveTo(
        IReadOnlyList<(int X, int Y)> path,
        int cost,
        IReadOnlyCollection<MapNode> nodes)
    {
        MapNode? triggered = null;
        var walked = 0;
        var spent = 0;
        var (previousX, previousY) = (PartyX, PartyY);

        foreach (var (x, y) in path)
        {
            // Re-priced per step rather than reusing `cost`, because a truncated walk must be
            // charged for what it covered — same formula as FindPath's edge weight.
            spent += 1 + Math.Max(0, ElevationAt(x, y) - ElevationAt(previousX, previousY));
            (previousX, previousY) = (x, y);
            walked++;

            RevealAround(x, y, nodes);

            triggered = nodes.FirstOrDefault(node =>
                node.Lane == x && node.Row == y && node.TriggersOnContact && !node.IsHidden);

            if (triggered is not null)
            {
                break;
            }
        }

        if (walked > 0)
        {
            var (lastX, lastY) = path[walked - 1];
            PartyX = lastX;
            PartyY = lastY;
        }

        MovementBudgetRemaining -= triggered is null ? cost : spent;

        return triggered;
    }

    /// <summary>
    /// Spends budget on something other than walking (searching the ground — see
    /// <see cref="Room.SearchAtPartyPosition"/>), then re-reveals around the party: a node that
    /// just stopped hiding becomes visible without the party having to move again.
    /// </summary>
    internal void SpendMovementBudget(int cost, IReadOnlyCollection<MapNode> nodes)
    {
        MovementBudgetRemaining -= cost;
        RevealAround(PartyX, PartyY, nodes);
    }

    public void ResetToInitial(IReadOnlyCollection<MapNode> nodes)
    {
        PartyX = StartX;
        PartyY = StartY;
        MovementBudgetRemaining = MovementBudget;
        _revealedNodeIds.Clear();
        _revealedCells.Clear();
        RevealAround(StartX, StartY, nodes);
    }

    private void RevealAround(int x, int y, IReadOnlyCollection<MapNode> nodes)
    {
        for (var dx = -VisionRadius; dx <= VisionRadius; dx++)
        {
            for (var dy = -VisionRadius; dy <= VisionRadius; dy++)
            {
                if (Math.Abs(dx) + Math.Abs(dy) > VisionRadius)
                {
                    continue;
                }

                var cellX = x + dx;
                var cellY = y + dy;

                // A hole in the room's shape is not a cell you can reveal — there is nothing
                // there to see. (IsFloor also covers the bounds check.)
                if (!IsFloor(cellX, cellY))
                {
                    continue;
                }

                if (!HasLineOfSight(x, y, cellX, cellY))
                {
                    continue;
                }

                _revealedCells.Add((cellX, cellY));
            }
        }

        foreach (var mapNode in nodes)
        {
            if (Math.Abs(mapNode.Lane - x) + Math.Abs(mapNode.Row - y) <= VisionRadius
                && HasLineOfSight(x, y, mapNode.Lane, mapNode.Row))
            {
                _revealedNodeIds.Add(mapNode.Id);
            }
        }
    }

    /// <summary>
    /// Whether a viewer standing at (viewerX, viewerY) can see (targetX, targetY): obstacles
    /// always block, and tall terrain blocks once it rises above the straight sightline
    /// interpolated between the viewer's eye height (their own elevation + 1) and the target
    /// cell's ground level. At <see cref="VisionRadius"/> = 2, every candidate cell has at most
    /// one intermediate cell on its line, so this stays a cheap per-cell check rather than a
    /// general shadowcasting algorithm.
    /// </summary>
    private bool HasLineOfSight(int viewerX, int viewerY, int targetX, int targetY)
    {
        var line = BresenhamLine(viewerX, viewerY, targetX, targetY);

        if (line.Count <= 2)
        {
            return true;
        }

        var eyeHeight = ElevationAt(viewerX, viewerY) + 1;
        var targetHeight = ElevationAt(targetX, targetY);
        var totalSteps = line.Count - 1;

        for (var i = 1; i < line.Count - 1; i++)
        {
            var (intermediateX, intermediateY) = line[i];

            if (IsObstacle(intermediateX, intermediateY))
            {
                return false;
            }

            // A hole in the room is empty space, not a wall: you see straight across a gap, and
            // its stored elevation is meaningless, so skip the height test rather than let a
            // leftover value block the sightline.
            if (!IsFloor(intermediateX, intermediateY))
            {
                continue;
            }

            var t = (double)i / totalSteps;
            var sightlineHeight = eyeHeight + ((targetHeight - eyeHeight) * t);

            if (ElevationAt(intermediateX, intermediateY) > sightlineHeight)
            {
                return false;
            }
        }

        return true;
    }

    private static List<(int X, int Y)> BresenhamLine(int x0, int y0, int x1, int y1)
    {
        var points = new List<(int X, int Y)>();
        var dx = Math.Abs(x1 - x0);
        var dy = -Math.Abs(y1 - y0);
        var stepX = x0 < x1 ? 1 : -1;
        var stepY = y0 < y1 ? 1 : -1;
        var error = dx + dy;
        var x = x0;
        var y = y0;

        while (true)
        {
            points.Add((x, y));

            if (x == x1 && y == y1)
            {
                break;
            }

            var doubledError = 2 * error;

            if (doubledError >= dy)
            {
                error += dy;
                x += stepX;
            }

            if (doubledError <= dx)
            {
                error += dx;
                y += stepY;
            }
        }

        return points;
    }

    private int IndexOf(int x, int y) => (y * Width) + x;

    public static RoomGrid Rehydrate(
        int width,
        int height,
        int movementBudget,
        int movementBudgetRemaining,
        int startX,
        int startY,
        int partyX,
        int partyY,
        IEnumerable<NodeId> revealedNodeIds,
        IEnumerable<(int X, int Y)> revealedCells,
        IReadOnlyList<int> elevation,
        IReadOnlyCollection<(int X, int Y)> obstacles,
        IReadOnlyList<bool>? floorCells = null)
    {
        // Null = every cell is floor, matching rooms persisted before rooms had a shape (their
        // stored mask is empty and the mapper hands back null).
        var resolvedFloor = floorCells is null
            ? Enumerable.Repeat(true, width * height).ToArray()
            : floorCells.ToArray();

        return new RoomGrid(
            width, height, movementBudget, movementBudgetRemaining,
            startX, startY, partyX, partyY,
            new HashSet<NodeId>(revealedNodeIds), new HashSet<(int X, int Y)>(revealedCells),
            elevation.ToArray(), new HashSet<(int X, int Y)>(obstacles), resolvedFloor);
    }
}
