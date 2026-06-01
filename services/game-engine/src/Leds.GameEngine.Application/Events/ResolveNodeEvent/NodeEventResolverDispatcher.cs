using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Application.Events.ResolveNodeEvent;

public sealed class NodeEventResolverDispatcher : INodeEventResolverDispatcher
{
    private readonly IReadOnlyDictionary<Domain.Nodes.NodeEventType, INodeEventResolver> _resolvers;

    public NodeEventResolverDispatcher(IEnumerable<INodeEventResolver> resolvers)
    {
        _resolvers = resolvers.ToDictionary(
            resolver => resolver.EventType,
            resolver => resolver);
    }

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        var primaryEventType = context.Node.EventType;

        if (!_resolvers.TryGetValue(primaryEventType, out var resolver))
        {
            throw new DomainException($"No resolver registered for node event type '{primaryEventType}'.");
        }

        return resolver.Resolve(context);
    }
}