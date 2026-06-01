using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.ChoiceResolvers;

public sealed class LawEventChoiceResolver : ICurrentEventChoiceResolver
{
    public NodeEventType EventType => NodeEventType.Law;

    public CurrentEventChoiceResolutionResult Resolve(
        CurrentEventChoiceResolutionContext context)
    {
        return context.ChoiceId switch
        {
            "accept-law" => CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                "La Loi est acceptée par le Palais.",
                new[]
                {
                    new NarrativeFragmentDto(
                        "Elise",
                        "Une Loi acceptée devient une cicatrice avec des règles.")
                }),

            "reject-law" => CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                "La Loi est rejetée. Le Palais reste stable pour l’instant.",
                new[]
                {
                    new NarrativeFragmentDto(
                        "Elise",
                        "Refuser une Loi ne l’efface pas. Cela l’éloigne seulement.")
                }),

            _ => throw new DomainException(
                $"Choice '{context.ChoiceId}' is not valid for event type '{EventType}'.")
        };
    }
}