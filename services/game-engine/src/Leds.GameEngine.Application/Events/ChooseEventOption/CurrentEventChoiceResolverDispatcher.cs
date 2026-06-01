using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public sealed class CurrentEventChoiceResolverDispatcher
    : ICurrentEventChoiceResolverDispatcher
{
    private readonly IReadOnlyDictionary<NodeEventType, ICurrentEventChoiceResolver> _resolvers;

    public CurrentEventChoiceResolverDispatcher(
        IEnumerable<ICurrentEventChoiceResolver> resolvers)
    {
        _resolvers = resolvers.ToDictionary(
            resolver => resolver.EventType,
            resolver => resolver);
    }

    public CurrentEventChoiceResolutionResult Resolve(
        CurrentEventChoiceResolutionContext context)
    {
        var primaryEventType = context.Node.EventType;

        if (!_resolvers.TryGetValue(primaryEventType, out var resolver))
        {
            throw new DomainException(
                $"Current event type '{primaryEventType}' does not accept player choices.");
        }

        return resolver.Resolve(context);
    }
}