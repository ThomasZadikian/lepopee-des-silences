using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.ChoiceResolvers;

public sealed class LawEventChoiceResolver : ICurrentEventChoiceResolver
{
    private readonly IPalaceLawCatalog _palaceLawCatalog;

    public LawEventChoiceResolver(IPalaceLawCatalog palaceLawCatalog)
    {
        _palaceLawCatalog = palaceLawCatalog;
    }

    public NodeEventType EventType => NodeEventType.Law;

    public CurrentEventChoiceResolutionResult Resolve(
        CurrentEventChoiceResolutionContext context)
    {
        return context.ChoiceId switch
        {
            "accept-law" => AcceptLaw(context),
            "reject-law" => RejectLaw(context),
            _ => throw new DomainException(
                $"Choice '{context.ChoiceId}' is not valid for event type '{EventType}'.")
        };
    }

    private CurrentEventChoiceResolutionResult AcceptLaw(
        CurrentEventChoiceResolutionContext context)
    {
        // Guard: if no law engine is available, treat acceptance as a narrative-only outcome
        // rather than activating a law with no real effect. The static catalog always provides
        // a placeholder, so this guard is currently a safety net for future engine evolution.
        // (Spec constraint: Ne pas faire un Palace Law Engine complet.)
        var law = _palaceLawCatalog.GetDefaultLawFor(context);
        if (law is null)
        {
            return CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                "La Loi reste suspendue — aucun moteur n'est disponible pour l'appliquer.",
                [new NarrativeFragmentDto("Elise", "Certaines Lois existent sans pouvoir agir.")]);
        }

        context.Run.ActivatePalaceLaw(law);

        return CurrentEventChoiceResolutionResult.Create(
            context.ChoiceId,
            accepted: true,
            $"La {law.Name} est acceptée par le Palais.",
            new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Une Loi acceptée devient une cicatrice avec des règles.")
            });
    }

    private static CurrentEventChoiceResolutionResult RejectLaw(
        CurrentEventChoiceResolutionContext context)
    {
        return CurrentEventChoiceResolutionResult.Create(
            context.ChoiceId,
            accepted: true,
            "La Loi est rejetée. Le Palais reste stable pour l’instant.",
            new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Refuser une Loi ne l’efface pas. Cela l’éloigne seulement.")
            });
    }
}