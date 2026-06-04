using Leds.GameEngine.Domain.NodeEvents;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record NodeEventDto(
    string EventType,
    int Order,
    string Status)
{
    public static NodeEventDto FromDomain(NodeEvent nodeEvent)
    {
        return new NodeEventDto(
            nodeEvent.EventType.ToString(),
            nodeEvent.Order,
            nodeEvent.Status.ToString());
    }
}