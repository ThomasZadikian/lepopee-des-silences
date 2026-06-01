using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.ChoiceResolvers;

public sealed class MerchantEventChoiceResolver : ICurrentEventChoiceResolver
{
    public NodeEventType EventType => NodeEventType.Merchant;

    public CurrentEventChoiceResolutionResult Resolve(
        CurrentEventChoiceResolutionContext context)
    {
        return context.ChoiceId switch
        {
            "trade" => CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                "Le joueur accepte d’ouvrir une proposition d’échange.",
                new[]
                {
                    new NarrativeFragmentDto(
                        "Elise",
                        "Observe bien ce qu’il demande. Le prix n’est pas toujours écrit.")
                }),

            "refuse" => CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                "Le joueur refuse l’échange.",
                new[]
                {
                    new NarrativeFragmentDto(
                        "Elise",
                        "Refuser un marché est parfois la seule richesse qu’il te reste.")
                }),

            _ => throw new DomainException(
                $"Choice '{context.ChoiceId}' is not valid for event type '{EventType}'.")
        };
    }
}