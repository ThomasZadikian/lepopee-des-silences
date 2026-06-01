using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class RareNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.Rare;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.RareEventResolved,
            "Anomalie lumineuse",
            "Un événement rare déforme brièvement la logique du Palais.",
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Souviens-toi de cet endroit. Il ne reviendra peut-être jamais sous cette forme.")
            });
    }
}