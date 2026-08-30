namespace Leds.GameEngine.Application.Events.ResolveNodeEvent;

public interface INodeEventResolverDispatcher
{
    NodeEventResolutionResult Resolve(NodeEventResolutionContext context);
}