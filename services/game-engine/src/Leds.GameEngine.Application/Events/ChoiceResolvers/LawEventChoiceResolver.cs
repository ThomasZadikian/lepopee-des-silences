using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Events.ChoiceResolvers;

public sealed class LawEventChoiceResolver : ICurrentEventChoiceResolver
{
    private readonly IPalaceLawCatalog _palaceLawCatalog;
    private readonly ICatalogPalaceLawDefinitionProvider _lawDefinitionProvider;
    private readonly ICatalogEffectSetProvider _effectSetProvider;

    public LawEventChoiceResolver(
        IPalaceLawCatalog palaceLawCatalog,
        ICatalogPalaceLawDefinitionProvider lawDefinitionProvider,
        ICatalogEffectSetProvider effectSetProvider)
    {
        _palaceLawCatalog = palaceLawCatalog;
        _lawDefinitionProvider = lawDefinitionProvider;
        _effectSetProvider = effectSetProvider;
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
            "La Loi est rejetée. Le Palais reste stable pour l'instant.",
            new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Refuser une Loi ne l'efface pas. Cela l'éloigne seulement.")
            });
    }
}
