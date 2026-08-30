using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class NpcNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.Npc;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.NarrativeFragmentRevealed,
            "Rencontre incertaine",
            "Une figure du Palais attend une réponse.",
            requiresPlayerChoice: true,
            choices: new[]
            {
                new NodeEventChoiceDto(
                    "listen",
                    "Écouter",
                    "Révèle un fragment narratif."),
                new NodeEventChoiceDto(
                    "leave",
                    "S’éloigner",
                    "Préserve la stabilité de la run.")
            },
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Toutes les voix ici ne mentent pas. C’est cela qui les rend dangereuses.")
            });
    }
}