using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class CombatNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.Combat;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.CombatStarted,
            "Résonance hostile",
            "Une présence hostile se manifeste dans le Palais.",
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Le Palais ne t’attaque jamais sans raison.")
            });
    }
}