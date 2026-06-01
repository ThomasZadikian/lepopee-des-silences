using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class LawNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.Law;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.PalaceLawOffered,
            "Loi murmurée",
            "Une Loi du Palais cherche à s’imposer à la run.",
            requiresPlayerChoice: true,
            choices: new[]
            {
                new NodeEventChoiceDto(
                    "accept-law",
                    "Accepter la Loi",
                    "Altère durablement plusieurs systèmes de la run."),
                new NodeEventChoiceDto(
                    "reject-law",
                    "Refuser la Loi",
                    "Conserve la stabilité actuelle.")
            },
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Une Loi n’est jamais une règle. C’est une blessure qui a trouvé une forme.")
            });
    }
}