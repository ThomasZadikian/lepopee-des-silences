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
        var law = _palaceLawCatalog.GetDefaultLawFor(context);

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