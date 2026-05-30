using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Domain.Rooms;

public sealed class Room
{
    private readonly List<Node> _nodes;

    private Room(
        RoomId id,
        int depth,
        string theme,
        int maxNodeDepth,
        RoomState state,
        IEnumerable<Node> nodes)
    {
        Id = id;
        Depth = depth;
        Theme = theme;
        MaxNodeDepth = maxNodeDepth;
        State = state;
        CurrentNodeDepth = 0;
        _nodes = nodes.ToList();
    }

    public RoomId Id { get; }

    /// <summary>
    /// Profondeur de la room dans la run.
    /// </summary>
    public int Depth { get; }

    public string Theme { get; }

    /// <summary>
    /// Profondeur interne actuelle dans la room.
    /// </summary>
    public int CurrentNodeDepth { get; private set; }

    /// <summary>
    /// Profondeur interne maximale de la room.
    /// Le dernier niveau doit contenir le boss de room.
    /// </summary>
    public int MaxNodeDepth { get; }

    public RoomState State { get; private set; }

    public IReadOnlyCollection<Node> Nodes => _nodes.AsReadOnly();

    public IReadOnlyCollection<Node> AvailableNodes => _nodes
        .Where(node => node.NodeDepth == CurrentNodeDepth && node.State == NodeState.Available)
        .ToArray();

    public static Room Create(
        int depth,
        string theme,
        IEnumerable<Node> nodes,
        int maxNodeDepth = 3)
    {
        if (depth is < 0 or > 10)
        {
            throw new DomainException("Room depth must be between 0 and 10.");
        }

        if (string.IsNullOrWhiteSpace(theme))
        {
            throw new DomainException("Room theme is required.");
        }

        if (maxNodeDepth is < 1 or > 10)
        {
            throw new DomainException("Room max node depth must be between 1 and 10.");
        }

        var nodeList = nodes?.ToList()
            ?? throw new DomainException("Room nodes are required.");

        if (nodeList.Count is < 1 or > 70)
        {
            throw new DomainException("A room must contain between 1 and 70 nodes.");
        }

        if (nodeList.Any(node => node.NodeDepth != 0))
        {
            throw new DomainException("Initial room nodes must have node depth 0.");
        }

        if (nodeList.Any(node => node.IsRoomBossNode))
        {
            throw new DomainException("Initial room nodes cannot be room boss nodes.");
        }

        return new Room(
            RoomId.New(),
            depth,
            theme.Trim(),
            maxNodeDepth,
            RoomState.Active,
            nodeList);
    }

    public Node GetNode(NodeId nodeId)
    {
        return _nodes.FirstOrDefault(node => node.Id == nodeId)
            ?? throw new DomainException("Node does not belong to this room.");
    }

    public Node GetResolvedNodeAtCurrentDepth()
    {
        return _nodes.SingleOrDefault(node =>
                node.NodeDepth == CurrentNodeDepth &&
                node.State == NodeState.Resolved)
            ?? throw new DomainException("No node has been resolved at the current room depth.");
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

    public void AddNextNodes(IEnumerable<Node> nextNodes)
    {
        if (State != RoomState.NodeResolved)
        {
            throw new DomainException("Current node event must be resolved before adding next nodes.");
        }

        if (CurrentNodeDepth >= MaxNodeDepth)
        {
            throw new DomainException("Room has already reached its maximum node depth.");
        }

        var nextDepth = CurrentNodeDepth + 1;

        var nodeList = nextNodes?.ToList()
            ?? throw new DomainException("Next nodes are required.");

        if (nodeList.Count is < 1 or > 4)
        {
            throw new DomainException("Next node layer must contain between 1 and 4 nodes.");
        }

        if (nodeList.Any(node => node.NodeDepth != nextDepth))
        {
            throw new DomainException("Next nodes must target the next room node depth.");
        }

        if (nextDepth == MaxNodeDepth)
        {
            if (nodeList.Count != 1 || nodeList.Single().IsRoomBossNode is false)
            {
                throw new DomainException("The final room node depth must contain exactly one room boss node.");
            }
        }
        else if (nodeList.Any(node => node.IsRoomBossNode))
        {
            throw new DomainException("Room boss nodes can only appear at the final room node depth.");
        }

        _nodes.AddRange(nodeList);
        CurrentNodeDepth = nextDepth;

        State = CurrentNodeDepth == MaxNodeDepth
            ? RoomState.BossReached
            : RoomState.Active;
    }
}