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
        HashSet<(int X, int Y)> obstacles)
    {
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

    public int ElevationAt(int x, int y) => _elevation[(y * Width) + x];

    public bool IsObstacle(int x, int y) => _obstacles.Contains((x, y));

    public static RoomGrid CreateInitial(
        int width,
        int height,
        int movementBudget,
        int startX,
        int startY,
        IReadOnlyCollection<MapNode> nodes,
        IReadOnlyList<int>? elevation = null,
        IReadOnlyCollection<(int X, int Y)>? obstacles = null)
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

        var grid = new RoomGrid(
            width, height, movementBudget, movementBudget,
            startX, startY, startX, startY,
            new HashSet<NodeId>(), new HashSet<(int X, int Y)>(),
            resolvedElevation, resolvedObstacles);

        grid.RevealAround(startX, startY, nodes);

        return grid;
    }

    /// <summary>
    /// Finds the cheapest walkable route from the party's current position to
    /// (targetX, targetY), routing around obstacle cells entirely (never enqueued) and pricing
    /// each orthogonal step at 1 plus any elevation gained climbing into it (descending is free).
    /// Returns null when the target is out of bounds, itself an obstacle, or unreachable given
    /// the current obstacle layout. The returned path starts with the first step taken (excludes
    /// the party's current cell) and ends with the target cell.
    /// </summary>
    public (IReadOnlyList<(int X, int Y)> Path, int Cost)? FindPath(int targetX, int targetY)
    {
        if (targetX < 0 || targetX >= Width || targetY < 0 || targetY >= Height)
        {
            return null;
        }

        if (IsObstacle(targetX, targetY))
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

            foreach (var (dx, dy) in OrthogonalNeighbors)
            {
                var neighborX = currentX + dx;
                var neighborY = currentY + dy;

                if (neighborX < 0 || neighborX >= Width || neighborY < 0 || neighborY >= Height)
                {
                    continue;
                }

                if (IsObstacle(neighborX, neighborY))
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
    /// Moves the party along an already-validated <paramref name="path"/> (see
    /// <see cref="FindPath"/> — bounds, obstacles, and budget already checked by
    /// <see cref="Room.MoveParty"/>). Reveals fog of war at every cell walked through, not just
    /// the destination.
    /// </summary>
    public void MoveTo(IReadOnlyList<(int X, int Y)> path, int cost, IReadOnlyCollection<MapNode> nodes)
    {
        foreach (var (x, y) in path)
        {
            RevealAround(x, y, nodes);
        }

        if (path.Count > 0)
        {
            var (lastX, lastY) = path[^1];
            PartyX = lastX;
            PartyY = lastY;
        }

        MovementBudgetRemaining -= cost;
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

                if (cellX < 0 || cellX >= Width || cellY < 0 || cellY >= Height)
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
        IReadOnlyCollection<(int X, int Y)> obstacles)
    {
        return new RoomGrid(
            width, height, movementBudget, movementBudgetRemaining,
            startX, startY, partyX, partyY,
            new HashSet<NodeId>(revealedNodeIds), new HashSet<(int X, int Y)>(revealedCells),
            elevation.ToArray(), new HashSet<(int X, int Y)>(obstacles));
    }
}
