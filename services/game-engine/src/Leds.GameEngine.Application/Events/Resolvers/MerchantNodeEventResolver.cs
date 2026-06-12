using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class MerchantNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.Merchant;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.TradeOffered,
            "Marchand de fragments",
            "Une entité propose un échange dont le coût n'est pas entièrement visible.",
            requiresPlayerChoice: false,
            choices: new[]
            {
                new NodeEventChoiceDto(
                    "trade",
                    "Échanger",
                    "Ouvre une proposition de marchandage."),
                new NodeEventChoiceDto(
                    "refuse",
                    "Refuser",
                    "Ne modifie pas l'état de la run.")
            },
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Dans le Palais, rien ne s'achète seulement avec de la monnaie.")
            });
    }
}