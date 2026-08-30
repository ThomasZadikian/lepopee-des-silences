using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class CurseNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.Curse;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.CurseOffered,
            "Dette intérieure",
            "Une malédiction propose une puissance immédiate contre un coût futur.",
            requiresPlayerChoice: true,
            choices: new[]
            {
                new NodeEventChoiceDto(
                    "accept-curse",
                    "Accepter la malédiction",
                    "Augmente le risque et améliore la récompense potentielle."),
                new NodeEventChoiceDto(
                    "reject-curse",
                    "Refuser la malédiction",
                    "Évite le coût futur.")
            },
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Le Palais sait très bien déguiser une punition en récompense.")
            });
    }
}