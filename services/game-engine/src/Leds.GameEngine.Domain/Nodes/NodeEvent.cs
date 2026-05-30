using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Nodes;

public sealed class NodeEvent
{
    private NodeEvent(NodeEventType eventType, int order)
    {
        EventType = eventType;
        Order = order;
    }

    public NodeEventType EventType { get; }

    public int Order { get; }

    public static NodeEvent Create(NodeEventType eventType, int order)
    {
        if (order is < 1 or > 4)
        {
            throw new DomainException("Node event order must be between 1 and 4.");
        }

        return new NodeEvent(eventType, order);
    }
}