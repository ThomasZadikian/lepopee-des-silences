using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class EliteNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.Elite;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.EliteEncounterStarted,
            "Présence dominante",
            "Un traumatisme plus dense que les autres bloque le passage.",
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Certains silences ont appris à se défendre.")
            });
    }
}