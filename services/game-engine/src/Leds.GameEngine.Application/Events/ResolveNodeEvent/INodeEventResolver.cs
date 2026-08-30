using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.ResolveNodeEvent;

public interface INodeEventResolver
{
    NodeEventType EventType { get; }

    NodeEventResolutionResult Resolve(NodeEventResolutionContext context);
}