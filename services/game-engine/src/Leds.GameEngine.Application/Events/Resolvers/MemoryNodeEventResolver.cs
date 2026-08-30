using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class MemoryNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.Memory;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        var fragmentKey = $"memory.{context.Node.EventType.ToString().ToLowerInvariant()}.fragment-{context.Node.Id.Value.ToString()[..8]}";

        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.NarrativeFragmentRevealed,
            "Fragment de Mémoire",
            "Le silence conserve ce que le Palais a oublié.",
            requiresPlayerChoice: false,
            choices: [],
            narrativeFragments:
            [
                new NarrativeFragmentDto(
                    "Elise",
                    $"Un souvenir persiste ici, comme une trace laissée par le Palais. ({fragmentKey})")
            ]);
    }
}