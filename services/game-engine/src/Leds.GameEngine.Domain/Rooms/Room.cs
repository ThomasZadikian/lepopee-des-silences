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
        IEnumerable<Node> nodes)
    {
        Id = id;
        Depth = depth;
        Theme = theme;
        _nodes = nodes.ToList();
    }

    public RoomId Id { get; }

    public int Depth { get; }

    public string Theme { get; }

    public IReadOnlyCollection<Node> Nodes => _nodes.AsReadOnly();

    public static Room Create(
        int depth,
        string theme,
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

        var nodeList = nodes?.ToList() ?? throw new DomainException("Room nodes are required.");

        if (nodeList.Count is < 1 or > 70)
        {
            throw new DomainException("A room must contain between 1 and 70 nodes.");
        }

        return new Room(
            RoomId.New(),
            depth,
            theme.Trim(),
            nodeList);
    }

    public Node GetNode(NodeId nodeId)
    {
        return _nodes.FirstOrDefault(node => node.Id == nodeId)
            ?? throw new DomainException("Node does not belong to this room.");
    }

    public void SelectNode(NodeId nodeId)
    {
        var selectedNode = GetNode(nodeId);

        selectedNode.Select();

        foreach (var node in _nodes.Where(node => node.Id != nodeId))
        {
            node.Lock();
        }
    }
}