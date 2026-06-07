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
            NodeEventResolutionKind.RareCombatStarted,
            "Présence singulière",
            "Une entité rare du Palais bloque le passage. Ce combat promet des récompenses inhabituelles.",
            requiresPlayerChoice: false,
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Certaines choses ne se montrent qu'une fois. Affronte-la.")
            });
    }
}