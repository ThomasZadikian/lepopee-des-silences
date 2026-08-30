using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Domain.NodeEvents;

public sealed class NodeEvent
{
    private NodeEvent(
        NodeEventType eventType,
        int order,
        NodeEventStatus status)
    {
        EventType = eventType;
        Order = order;
        Status = status;
    }

    public NodeEventType EventType { get; }

    public int Order { get; }

    public NodeEventStatus Status { get; private set; }

    public bool IsPlanned => Status == NodeEventStatus.Planned;

    public bool IsResolved => Status == NodeEventStatus.Resolved;

    public bool IsClosed => Status == NodeEventStatus.Closed;

    public bool IsTerminal => Status is NodeEventStatus.Resolved or NodeEventStatus.Closed;

    public static NodeEvent Create(NodeEventType eventType, int order)
    {
        if (order is < 1 or > 4)
        {
            throw new DomainException("Node event order must be between 1 and 4.");
        }

        return new NodeEvent(
            eventType,
            order,
            NodeEventStatus.Planned);
    }

    public void Resolve()
    {
        if (Status == NodeEventStatus.Resolved)
        {
            throw new DomainException("Node event is already resolved.");
        }

        if (Status == NodeEventStatus.Closed)
        {
            throw new DomainException("Closed node event cannot be resolved.");
        }

        Status = NodeEventStatus.Resolved;
    }

    public void Close()
    {
        if (Status == NodeEventStatus.Resolved)
        {
            throw new DomainException("Resolved node event cannot be closed.");
        }

        if (Status == NodeEventStatus.Closed)
        {
            return;
        }

        Status = NodeEventStatus.Closed;
    }
}