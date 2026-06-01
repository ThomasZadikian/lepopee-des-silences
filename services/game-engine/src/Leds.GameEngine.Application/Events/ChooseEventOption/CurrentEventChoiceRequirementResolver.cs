using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public sealed class CurrentEventChoiceRequirementResolver
    : ICurrentEventChoiceRequirementResolver
{
    private readonly IReadOnlySet<NodeEventType> _eventTypesRequiringChoice;

    public CurrentEventChoiceRequirementResolver(
        IEnumerable<ICurrentEventChoiceResolver> choiceResolvers)
    {
        _eventTypesRequiringChoice = choiceResolvers
            .Select(resolver => resolver.EventType)
            .ToHashSet();
    }

    public bool RequiresChoice(Node node)
    {
        return _eventTypesRequiringChoice.Contains(node.EventType);
    }
}