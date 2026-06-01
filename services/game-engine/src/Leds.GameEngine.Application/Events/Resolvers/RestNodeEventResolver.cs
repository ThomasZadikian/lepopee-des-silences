using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class RestNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.Rest;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.RestResolved,
            "Respiration du Palais",
            "Le silence se stabilise. Le joueur récupère partiellement.",
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Se reposer ici n’est pas fuir. C’est refuser de se briser.")
            });
    }
}