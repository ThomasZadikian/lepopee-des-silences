using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Domain.Rooms;

public sealed class Room
{
    private readonly List<Node> _nodes;

    private Room(
        RoomId id,
        int depth,
        RoomType roomType,
        string theme,
        RoomBossProfile bossProfile,
        RoomState state,
        IEnumerable<Node> nodes)
    {
        Id = id;
        Depth = depth;
        RoomType = roomType;
        Theme = theme;
        BossProfile = bossProfile;
        State = state;
        CurrentNodeDepth = 0;
        _nodes = nodes.ToList();
        MaxNodeDepth = _nodes.Max(node => node.NodeDepth);
    }

    public RoomId Id { get; }

    public int Depth { get; }

    public RoomType RoomType { get; }

    public string Theme { get; }

    public RoomBossProfile BossProfile { get; }

    public int CurrentNodeDepth { get; private set; }

    public int MaxNodeDepth { get; }

    public int TotalNodeCount => _nodes.Count;

    public RoomState State { get; private set; }

    public IReadOnlyCollection<Node> Nodes => _nodes.AsReadOnly();

    public IReadOnlyCollection<Node> AvailableNodes => _nodes
        .Where(node => node.NodeDepth == CurrentNodeDepth && node.State == NodeState.Available)
        .ToArray();

    public static Room Create(
        int depth,
        RoomType roomType,
        string theme,
        RoomBossProfile bossProfile,
        IEnumerable<Node> nodes)
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

        if (nodeList.Count is < 6 or > 10)
        {
            throw new DomainException("A room must contain between 6 and 10 nodes.");
        }

        var maxNodeDepth = nodeList.Max(node => node.NodeDepth);

        if (maxNodeDepth < 1)
        {
            throw new DomainException("A room must contain at least one progression layer before the boss.");
        }

        var bossNodes = nodeList.Where(node => node.IsRoomBossNode).ToArray();

        if (bossNodes.Length != 1)
        {
            throw new DomainException("A room must contain exactly one room boss node.");
        }

        var bossNode = bossNodes.Single();

        if (bossNode.NodeDepth != maxNodeDepth)
        {
            throw new DomainException("The room boss node must be placed at the final node depth.");
        }

        if (bossNode.State != NodeState.Planned)
        {
            throw new DomainException("The room boss node must start as planned.");
        }

        var finalLayerNodes = nodeList
        .Where(node => node.NodeDepth == maxNodeDepth)
        .ToArray();

        if (finalLayerNodes.Length != 1 || !finalLayerNodes.Single().IsRoomBossNode)
        {
            throw new DomainException("The final node depth must contain exactly one room boss node.");
        }

        var initialNodes = nodeList.Where(node => node.NodeDepth == 0).ToArray();

        if (initialNodes.Length is < 1 or > 4)
        {
            throw new DomainException("Initial node layer must contain between 1 and 4 nodes.");
        }

        if (initialNodes.Any(node => node.State != NodeState.Available))
        {
            throw new DomainException("Initial node layer must start as available.");
        }

        var futureNodes = nodeList.Where(node => node.NodeDepth > 0).ToArray();

        if (futureNodes.Any(node => node.State != NodeState.Planned))
        {
            throw new DomainException("Future node layers must start as planned.");
        }

        var groupedLayers = nodeList.GroupBy(node => node.NodeDepth).ToArray();

        if (groupedLayers.Any(group => group.Count() is < 1 or > 4))
        {
            throw new DomainException("Each node layer must contain between 1 and 4 nodes.");
        }

        EnsureLayerDepthsAreContinuous(nodeList, maxNodeDepth);
        EnsureParentReferencesAreValid(nodeList);
        EnsureAllNonBossNodesHaveChildren(nodeList, maxNodeDepth);
        EnsureAllPathsConvergeToBoss(nodeList, bossNode);

        return new Room(
            RoomId.New(),
            depth,
            roomType,
            theme.Trim(),
            bossProfile,
            RoomState.Active,
            nodeList);
    }

    public Node GetNode(NodeId nodeId)
    {
        return _nodes.FirstOrDefault(node => node.Id == nodeId)
            ?? throw new DomainException("Node does not belong to this room.");
    }

    public void SelectNode(NodeId nodeId)
    {
        if (State is not RoomState.Active and not RoomState.BossReached)
        {
            throw new DomainException("Room is not waiting for a node selection.");
        }

        var selectedNode = GetNode(nodeId);

        if (selectedNode.NodeDepth != CurrentNodeDepth)
        {
            throw new DomainException("Only a node from the current room depth can be selected.");
        }

        selectedNode.Select();

        foreach (var node in _nodes.Where(node =>
                     node.NodeDepth == CurrentNodeDepth &&
                     node.Id != nodeId))
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

        var selectedNode = _nodes.SingleOrDefault(node =>
                node.NodeDepth == CurrentNodeDepth &&
                node.State == NodeState.Selected)
            ?? throw new DomainException("No node has been selected for the current room depth.");

        selectedNode.Resolve();

        State = selectedNode.IsRoomBossNode
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

        var resolvedNode = _nodes.SingleOrDefault(node =>
                node.NodeDepth == CurrentNodeDepth &&
                node.State == NodeState.Resolved)
            ?? throw new DomainException("No node has been resolved at the current room depth.");

        var nextDepth = CurrentNodeDepth + 1;

        var nextLayerNodes = _nodes
            .Where(node =>
                node.NodeDepth == nextDepth &&
                node.State == NodeState.Planned &&
                node.ParentNodeIds.Contains(resolvedNode.Id))
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

        State = nextLayerNodes.Any(node => node.IsRoomBossNode)
            ? RoomState.BossReached
            : RoomState.Active;
    }

    private void MarkUnreachableBranches(Node selectedNode)
    {
        foreach (var node in _nodes.Where(node => node.NodeDepth > selectedNode.NodeDepth))
        {
            if (!IsReachableFrom(selectedNode.Id, node))
            {
                node.MarkUnreachable();
            }
        }
    }

    private bool IsReachableFrom(NodeId ancestorNodeId, Node node)
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

    private static void EnsureLayerDepthsAreContinuous(
        IReadOnlyCollection<Node> nodes,
        int maxNodeDepth)
    {
        var depths = nodes
            .Select(node => node.NodeDepth)
            .Distinct()
            .Order()
            .ToArray();

        var expectedDepths = Enumerable.Range(0, maxNodeDepth + 1).ToArray();

        if (!depths.SequenceEqual(expectedDepths))
        {
            throw new DomainException("Room node depths must be continuous from 0 to the boss depth.");
        }
    }

    private static void EnsureParentReferencesAreValid(IReadOnlyCollection<Node> nodes)
    {
        var nodesById = nodes.ToDictionary(node => node.Id);

        foreach (var node in nodes)
        {
            if (node.NodeDepth == 0 && node.ParentNodeIds.Count != 0)
            {
                throw new DomainException("Initial layer nodes cannot have parents.");
            }

            if (node.NodeDepth > 0 && node.ParentNodeIds.Count == 0)
            {
                throw new DomainException("Non-initial nodes must have at least one parent.");
            }

            foreach (var parentNodeId in node.ParentNodeIds)
            {
                if (!nodesById.TryGetValue(parentNodeId, out var parent))
                {
                    throw new DomainException("Node parent reference does not belong to this room.");
                }

                if (parent.NodeDepth != node.NodeDepth - 1)
                {
                    throw new DomainException("Node parents must belong to the previous node depth.");
                }

                if (parent.IsRoomBossNode)
                {
                    throw new DomainException("Room boss node cannot be used as a parent.");
                }
            }
        }
    }

    private static void EnsureAllNonBossNodesHaveChildren(
        IReadOnlyCollection<Node> nodes,
        int maxNodeDepth)
    {
        foreach (var node in nodes.Where(node => !node.IsRoomBossNode))
        {
            if (node.NodeDepth >= maxNodeDepth)
            {
                throw new DomainException("Only the room boss node can exist at the final node depth.");
            }

            var hasChild = nodes.Any(candidate =>
                candidate.NodeDepth == node.NodeDepth + 1 &&
                candidate.ParentNodeIds.Contains(node.Id));

            if (!hasChild)
            {
                throw new DomainException("Every non-boss node must have at least one child.");
            }
        }
    }

    private static void EnsureAllPathsConvergeToBoss(
        IReadOnlyCollection<Node> nodes,
        Node bossNode)
    {
        foreach (var node in nodes.Where(node => !node.IsRoomBossNode))
        {
            if (!HasPathToBoss(node, bossNode, nodes))
            {
                throw new DomainException("Every node must have a valid path to the room boss.");
            }
        }
    }

    private static bool HasPathToBoss(
        Node currentNode,
        Node bossNode,
        IReadOnlyCollection<Node> nodes)
    {
        var children = nodes
            .Where(node => node.ParentNodeIds.Contains(currentNode.Id))
            .ToArray();

        if (children.Any(child => child.Id == bossNode.Id))
        {
            return true;
        }

        return children.Any(child => HasPathToBoss(child, bossNode, nodes));
    }
}