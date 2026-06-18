using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Domain.Rooms;

public sealed class Room
{
    private readonly List<MapNode> _nodes;

    private Room(
        RoomId id,
        int depth,
        RoomType roomType,
        PalaceRoomState palaceState,
        string theme,
        RoomBossProfile bossProfile,
        RoomState state,
        IEnumerable<MapNode> nodes,
        string? layoutTemplateKey,
        string? layoutTemplateVersion)
    {
        Id = id;
        Depth = depth;
        RoomType = roomType;
        PalaceState = palaceState;
        Theme = theme;
        BossProfile = bossProfile;
        State = state;
        CurrentNodeDepth = 0;
        _nodes = nodes.ToList();
        MaxNodeDepth = _nodes.Count > 0 ? _nodes.Max(n => n.Row) : 0;
        LayoutTemplateKey = layoutTemplateKey;
        LayoutTemplateVersion = layoutTemplateVersion;
    }

    public RoomId Id { get; }

    public int Depth { get; }

    public RoomType RoomType { get; }

    public PalaceRoomState PalaceState { get; private set; }

    public string Theme { get; }

    public RoomBossProfile BossProfile { get; }

    public int CurrentNodeDepth { get; private set; }

    public int MaxNodeDepth { get; }

    public int TotalNodeCount => _nodes.Count;

    public RoomState State { get; private set; }

    public IReadOnlyCollection<MapNode> Nodes => _nodes.AsReadOnly();

    public string? LayoutTemplateKey { get; }

    public string? LayoutTemplateVersion { get; }

    public IReadOnlyCollection<MapNode> AvailableNodes => _nodes
        .Where(n => n.Row == CurrentNodeDepth && n.State == NodeState.Available)
        .ToArray();

    public static Room Create(
        int depth,
        RoomType roomType,
        PalaceRoomState palaceState,
        string theme,
        RoomBossProfile bossProfile,
        IEnumerable<MapNode> nodes)
    {
        if (depth is < 0 or > 10)
        {
            throw new DomainException("Room depth must be between 0 and 10.");
        }

        if (string.IsNullOrWhiteSpace(theme))
        {
            throw new DomainException("Room theme is required.");
        }

        ArgumentNullException.ThrowIfNull(bossProfile);

        var nodeList = nodes?.ToList()
            ?? throw new DomainException("Room nodes are required.");

        if (nodeList.Count < 2)
        {
            throw new DomainException("A room must contain at least 2 nodes.");
        }

        var maxRow = nodeList.Max(n => n.Row);

        var bossNodes = nodeList.Where(n => n.IsBoss).ToArray();

        if (bossNodes.Length != 1)
        {
            throw new DomainException("A room must contain exactly one boss node.");
        }

        var bossNode = bossNodes.Single();

        if (bossNode.Row != maxRow)
        {
            throw new DomainException("The boss node must be placed at the final row.");
        }

        if (bossNode.State != NodeState.Planned)
        {
            throw new DomainException("The boss node must start as planned.");
        }

        var finalRowNodes = nodeList.Where(n => n.Row == maxRow).ToArray();

        if (finalRowNodes.Length != 1 || !finalRowNodes.Single().IsBoss)
        {
            throw new DomainException("The final row must contain exactly one boss node.");
        }

        var initialNodes = nodeList.Where(n => n.Row == 0).ToArray();

        if (initialNodes.Length < 1)
        {
            throw new DomainException("Initial row must contain at least 1 node.");
        }

        if (initialNodes.Any(n => n.State != NodeState.Available))
        {
            throw new DomainException("Initial row nodes must start as available.");
        }

        var futureNodes = nodeList.Where(n => n.Row > 0).ToArray();

        if (futureNodes.Any(n => n.State != NodeState.Planned))
        {
            throw new DomainException("Future row nodes must start as planned.");
        }

        EnsureRowsAreContinuous(nodeList, maxRow);
        EnsureParentReferencesAreValid(nodeList);
        EnsureAllNonBossNodesHaveChildren(nodeList, maxRow);
        EnsureAllPathsConvergeToBoss(nodeList, bossNode);
        EnsureNoCrossRowConnections(nodeList);

        return new Room(
            RoomId.New(),
            depth,
            roomType,
            palaceState,
            theme.Trim(),
            bossProfile,
            RoomState.Active,
            nodeList,
            layoutTemplateKey: null,
            layoutTemplateVersion: null);
    }

    public static Room CreateFromTemplate(
        int depth,
        RoomType roomType,
        PalaceRoomState palaceState,
        string theme,
        RoomBossProfile bossProfile,
        IEnumerable<MapNode> nodes,
        string layoutTemplateKey,
        string layoutTemplateVersion)
    {
        var room = Create(depth, roomType, palaceState, theme, bossProfile, nodes);

        if (string.IsNullOrWhiteSpace(layoutTemplateKey))
        {
            throw new DomainException("Layout template key is required.");
        }

        if (string.IsNullOrWhiteSpace(layoutTemplateVersion))
        {
            throw new DomainException("Layout template version is required.");
        }

        return new Room(
            room.Id,
            room.Depth,
            room.RoomType,
            room.PalaceState,
            room.Theme,
            room.BossProfile,
            room.State,
            room._nodes,
            layoutTemplateKey.Trim(),
            layoutTemplateVersion.Trim());
    }

    public MapNode GetNode(NodeId nodeId)
    {
        return _nodes.FirstOrDefault(n => n.Id == nodeId)
            ?? throw new DomainException("Node does not belong to this room.");
    }

    public void SelectNode(NodeId nodeId)
    {
        if (State is not RoomState.Active and not RoomState.BossReached)
        {
            throw new DomainException("Room is not waiting for a node selection.");
        }

        var selectedNode = GetNode(nodeId);

        if (selectedNode.Row != CurrentNodeDepth)
        {
            throw new DomainException("Only a node from the current room depth can be selected.");
        }

        selectedNode.Select();

        foreach (var node in _nodes.Where(n =>
                     n.Row == CurrentNodeDepth && n.Id != nodeId))
        {
            node.Lock();
        }

        MarkUnreachableBranches(selectedNode);

        State = RoomState.NodeSelected;
    }

    public void ResolveSelectedNodeEvent()
    {
        if (State != RoomState.NodeSelected)
        {
            throw new DomainException("Room must have a selected node before resolving an event.");
        }

        var selectedNode = _nodes.SingleOrDefault(n =>
                n.Row == CurrentNodeDepth && n.State == NodeState.Selected)
            ?? throw new DomainException("No node has been selected for the current room depth.");

        selectedNode.Resolve();

        State = selectedNode.IsBoss
            ? RoomState.Completed
            : RoomState.NodeResolved;
    }

    public void UnlockNextNodeLayer()
    {
        if (State != RoomState.NodeResolved)
        {
            throw new DomainException("Current node event must be resolved before progressing.");
        }

        if (CurrentNodeDepth >= MaxNodeDepth)
        {
            throw new DomainException("Room has already reached its final node depth.");
        }

        var resolvedNode = _nodes.SingleOrDefault(n =>
                n.Row == CurrentNodeDepth && n.State == NodeState.Resolved)
            ?? throw new DomainException("No node has been resolved at the current room depth.");

        var nextDepth = CurrentNodeDepth + 1;

        var nextLayerNodes = _nodes
            .Where(n =>
                n.Row == nextDepth &&
                n.State == NodeState.Planned &&
                n.ParentNodeIds.Contains(resolvedNode.Id))
            .ToArray();

        if (nextLayerNodes.Length == 0)
        {
            throw new DomainException("Resolved node has no reachable child in the next node layer.");
        }

        foreach (var node in nextLayerNodes)
        {
            node.Unlock();
        }

        CurrentNodeDepth = nextDepth;

        State = nextLayerNodes.Any(n => n.IsBoss)
            ? RoomState.BossReached
            : RoomState.Active;
    }

    public void ResetProgress()
    {
        if (State is RoomState.Active or RoomState.NodeSelected
            or RoomState.NodeResolved or RoomState.BossReached)
        {
            foreach (var node in _nodes)
            {
                node.ResetToInitial();
            }

            CurrentNodeDepth = 0;
            State = RoomState.Active;
            return;
        }

        throw new DomainException("Room is closed and cannot be reset.");
    }

    private void MarkUnreachableBranches(MapNode selectedNode)
    {
        foreach (var node in _nodes.Where(n => n.Row > selectedNode.Row))
        {
            if (!IsReachableFrom(selectedNode.Id, node))
            {
                node.MarkUnreachable();
            }
        }
    }

    private bool IsReachableFrom(NodeId ancestorNodeId, MapNode node)
    {
        if (node.ParentNodeIds.Contains(ancestorNodeId))
        {
            return true;
        }

        foreach (var parentNodeId in node.ParentNodeIds)
        {
            var parent = _nodes.FirstOrDefault(candidate => candidate.Id == parentNodeId);

            if (parent is not null && IsReachableFrom(ancestorNodeId, parent))
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureRowsAreContinuous(
        IReadOnlyCollection<MapNode> nodes, int maxRow)
    {
        var rows = nodes
            .Select(n => n.Row)
            .Distinct()
            .Order()
            .ToArray();

        var expectedRows = Enumerable.Range(0, maxRow + 1).ToArray();

        if (!rows.SequenceEqual(expectedRows))
        {
            throw new DomainException("Room rows must be continuous from 0 to the boss row.");
        }
    }

    private static void EnsureParentReferencesAreValid(IReadOnlyCollection<MapNode> nodes)
    {
        var nodesById = nodes.ToDictionary(n => n.Id);

        foreach (var node in nodes)
        {
            if (node.Row == 0 && node.ParentNodeIds.Count != 0)
            {
                throw new DomainException("Initial row nodes cannot have parents.");
            }

            foreach (var parentNodeId in node.ParentNodeIds)
            {
                if (!nodesById.TryGetValue(parentNodeId, out var parent))
                {
                    throw new DomainException("Node parent reference does not belong to this room.");
                }

                if (parent.Row != node.Row - 1)
                {
                    throw new DomainException("Node parents must belong to the previous row.");
                }

                if (parent.IsBoss)
                {
                    throw new DomainException("Boss node cannot be used as a parent.");
                }
            }
        }
    }

    private static void EnsureAllNonBossNodesHaveChildren(
        IReadOnlyCollection<MapNode> nodes, int maxRow)
    {
        foreach (var node in nodes.Where(n => !n.IsBoss))
        {
            if (node.Row >= maxRow)
            {
                throw new DomainException("Only the boss node can exist at the final row.");
            }

            var hasChild = nodes.Any(candidate =>
                candidate.Row == node.Row + 1 &&
                candidate.ParentNodeIds.Contains(node.Id));

            if (!hasChild)
            {
                throw new DomainException("Every non-boss node must have at least one child.");
            }
        }
    }

    private static void EnsureAllPathsConvergeToBoss(
        IReadOnlyCollection<MapNode> nodes, MapNode bossNode)
    {
        foreach (var node in nodes.Where(n => !n.IsBoss))
        {
            if (!HasPathToBoss(node, bossNode, nodes))
            {
                throw new DomainException("Every node must have a valid path to the boss.");
            }
        }
    }

    private static bool HasPathToBoss(
        MapNode currentNode, MapNode bossNode, IReadOnlyCollection<MapNode> nodes)
    {
        var children = nodes
            .Where(n => n.ParentNodeIds.Contains(currentNode.Id))
            .ToArray();

        if (children.Any(c => c.Id == bossNode.Id))
        {
            return true;
        }

        return children.Any(c => HasPathToBoss(c, bossNode, nodes));
    }

    private static void EnsureNoCrossRowConnections(IReadOnlyCollection<MapNode> nodes)
    {
        var nodesById = nodes.ToDictionary(n => n.Id);

        foreach (var node in nodes)
        {
            foreach (var parentNodeId in node.ParentNodeIds)
            {
                if (!nodesById.TryGetValue(parentNodeId, out var parent))
                {
                    continue;
                }

                if (Math.Abs(parent.Lane - node.Lane) > 1)
                {
                    throw new DomainException(
                        "Node connections must be between same or adjacent lanes only.");
                }
            }
        }
    }

    public static Room Rehydrate(
        RoomId id,
        int depth,
        RoomType roomType,
        PalaceRoomState palaceState,
        string theme,
        RoomBossProfile bossProfile,
        RoomState state,
        int currentNodeDepth,
        IEnumerable<MapNode> nodes,
        string? layoutTemplateKey,
        string? layoutTemplateVersion)
    {
        var room = new Room(id, depth, roomType, palaceState, theme, bossProfile, state, nodes, layoutTemplateKey, layoutTemplateVersion);
        room.CurrentNodeDepth = currentNodeDepth;
        return room;
    }

    public void DebugSetPalaceState(PalaceRoomState palaceState)
    {
        PalaceState = palaceState;
    }

    public static Room Create(
        int depth,
        RoomType roomType,
        string theme,
        RoomBossProfile bossProfile,
        IEnumerable<MapNode> nodes)
    {
        return Create(depth, roomType, PalaceRoomState.Neutral, theme, bossProfile, nodes);
    }

    public static Room CreateFromTemplate(
        int depth,
        RoomType roomType,
        string theme,
        RoomBossProfile bossProfile,
        IEnumerable<MapNode> nodes,
        string layoutTemplateKey,
        string layoutTemplateVersion)
    {
        return CreateFromTemplate(
            depth,
            roomType,
            PalaceRoomState.Neutral,
            theme,
            bossProfile,
            nodes,
            layoutTemplateKey,
            layoutTemplateVersion);
    }

    public static Room Rehydrate(
        RoomId id,
        int depth,
        RoomType roomType,
        string theme,
        RoomBossProfile bossProfile,
        RoomState state,
        int currentNodeDepth,
        IEnumerable<MapNode> nodes,
        string? layoutTemplateKey,
        string? layoutTemplateVersion)
    {
        return Rehydrate(
            id,
            depth,
            roomType,
            PalaceRoomState.Neutral,
            theme,
            bossProfile,
            state,
            currentNodeDepth,
            nodes,
            layoutTemplateKey,
            layoutTemplateVersion);
    }
}
