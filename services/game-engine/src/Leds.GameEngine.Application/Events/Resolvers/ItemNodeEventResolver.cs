using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class ItemNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.Item;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.RewardGranted,
            "Éclat retrouvé",
            "Un objet mental est extrait de la pièce.",
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Même les ruines gardent parfois quelque chose d’utile.")
            });
    }
}