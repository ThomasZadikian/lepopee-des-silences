using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.ChoiceResolvers;

public sealed class CurseEventChoiceResolver : ICurrentEventChoiceResolver
{
    public NodeEventType EventType => NodeEventType.Curse;

    public CurrentEventChoiceResolutionResult Resolve(
        CurrentEventChoiceResolutionContext context)
    {
        return context.ChoiceId switch
        {
            "accept-curse" => CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                "La malédiction est acceptée. Son coût sera résolu dans une étape ultérieure.",
                new[]
                {
                    new NarrativeFragmentDto(
                        "Elise",
                        "Le Palais sait très bien appeler puissance ce qui commence par te prendre quelque chose.")
                }),

            "reject-curse" => CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                "La malédiction est refusée.",
                new[]
                {
                    new NarrativeFragmentDto(
                        "Elise",
                        "Toutes les portes ne méritent pas d’être ouvertes.")
                }),

            _ => throw new DomainException(
                $"Choice '{context.ChoiceId}' is not valid for event type '{EventType}'.")
        };
    }
}