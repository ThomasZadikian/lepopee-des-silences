using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.ChoiceResolvers;

public sealed class NpcEventChoiceResolver : ICurrentEventChoiceResolver
{
    public NodeEventType EventType => NodeEventType.Npc;

    public CurrentEventChoiceResolutionResult Resolve(
        CurrentEventChoiceResolutionContext context)
    {
        return context.ChoiceId switch
        {
            "listen" => CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                "Le joueur écoute la figure du Palais.",
                new[]
                {
                    new NarrativeFragmentDto(
                        "Elise",
                        "Écouter n’est pas toujours obéir.")
                }),

            "leave" => CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                "Le joueur s’éloigne de la rencontre.",
                new[]
                {
                    new NarrativeFragmentDto(
                        "Elise",
                        "Il y a des voix qu’il faut laisser derrière soi.")
                }),

            _ => throw new DomainException(
                $"Choice '{context.ChoiceId}' is not valid for event type '{EventType}'.")
        };
    }
}