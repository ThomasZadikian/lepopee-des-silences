using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Domain.Rooms;

/// <summary>
/// Free-movement grid overlay for a room. Owned by <see cref="Room"/> (<see cref="Room.Grid"/>).
/// Movement is orthogonal (4 directions), cost is Manhattan distance; fog of war reveals
/// cells/nodes within <see cref="VisionRadius"/> of every cell the party has stood on or passed
/// through.
/// </summary>
public sealed class RoomGrid
{
    // BALANCE KNOB — how many cells around the party are revealed by fog of war. Not
    // configurable per-room in v1.
    public const int VisionRadius = 2;

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
        HashSet<(int X, int Y)> revealedCells)
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

    public static RoomGrid CreateInitial(
        int width,
        int height,
        int movementBudget,
        int startX,
        int startY,
        IReadOnlyCollection<MapNode> nodes)
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

        var grid = new RoomGrid(
            width, height, movementBudget, movementBudget,
            startX, startY, startX, startY,
            new HashSet<NodeId>(), new HashSet<(int X, int Y)>());

        grid.RevealAround(startX, startY, nodes);

        return grid;
    }

    /// <summary>
    /// Moves the party to (targetX, targetY) for the given cost (already validated by
    /// <see cref="Room.MoveParty"/> — bounds, budget, adjacency). Reveals fog of war along an
    /// L-shaped path (X axis first, then Y) between the current and target position, not just
    /// at the destination, since there are no obstacles to path around in v1.
    /// </summary>
    public void MoveTo(int targetX, int targetY, int cost, IReadOnlyCollection<MapNode> nodes)
    {
        var stepX = Math.Sign(targetX - PartyX);
        for (var x = PartyX; x != targetX; x += stepX)
        {
            RevealAround(x, PartyY, nodes);
        }

        var stepY = Math.Sign(targetY - PartyY);
        for (var y = PartyY; y != targetY; y += stepY)
        {
            RevealAround(targetX, y, nodes);
        }

        PartyX = targetX;
        PartyY = targetY;
        MovementBudgetRemaining -= cost;

        RevealAround(targetX, targetY, nodes);
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

                _revealedCells.Add((cellX, cellY));
            }
        }

        foreach (var node in nodes)
        {
            if (Math.Abs(node.Lane - x) + Math.Abs(node.Row - y) <= VisionRadius)
            {
                _revealedNodeIds.Add(node.Id);
            }
        }
    }

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
        IEnumerable<(int X, int Y)> revealedCells)
    {
        return new RoomGrid(
            width, height, movementBudget, movementBudgetRemaining,
            startX, startY, partyX, partyY,
            new HashSet<NodeId>(revealedNodeIds), new HashSet<(int X, int Y)>(revealedCells));
    }
}
