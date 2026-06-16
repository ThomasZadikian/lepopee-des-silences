using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Events.ChoiceResolvers;

public sealed class CurseEventChoiceResolver : ICurrentEventChoiceResolver
{
    private readonly ICatalogCurseDefinitionProvider _curseDefinitionProvider;
    private readonly ICatalogEffectSetProvider _effectSetProvider;

    public CurseEventChoiceResolver(
        ICatalogCurseDefinitionProvider curseDefinitionProvider,
        ICatalogEffectSetProvider effectSetProvider)
    {
        _curseDefinitionProvider = curseDefinitionProvider;
        _effectSetProvider = effectSetProvider;
    }

    public NodeEventType EventType => NodeEventType.Curse;

    public CurrentEventChoiceResolutionResult Resolve(
        CurrentEventChoiceResolutionContext context)
    {
        return context.ChoiceId switch
        {
            "accept-curse" => AcceptCurse(context),
            "reject-curse" => RejectCurse(context),
            _ => throw new DomainException(
                $"Choice '{context.ChoiceId}' is not valid for event type '{EventType}'.")
        };
    }

    private CurrentEventChoiceResolutionResult AcceptCurse(
        CurrentEventChoiceResolutionContext context)
    {
        var curseKey = $"curse.{context.Node.Id.Value.ToString()[..8]}";

        var curse = ActiveCurse.Create(
            curseKey,
            "Malédiction acceptée",
            "Le coût sera résolu au prochain combat.",
            0.10,
            DateTime.UtcNow);

        context.Run.ApplyCurse(curse);

        var curseModifier = RunModifier.Create(
            RunModifierType.NextCombatDifficultyMultiplier,
            0.10,
            RunModifierDuration.NextCombatOnly,
            "Curse",
            curseKey);
        context.Run.AddRunModifier(curseModifier);

        return CurrentEventChoiceResolutionResult.Create(
            context.ChoiceId,
            accepted: true,
            "La malédiction est acceptée. Son coût sera résolu dans une étape ultérieure.",
            new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Le Palais sait très bien appeler puissance ce qui commence par te prendre quelque chose.")
            });
    }

    private static CurrentEventChoiceResolutionResult RejectCurse(
        CurrentEventChoiceResolutionContext context)
    {
        return CurrentEventChoiceResolutionResult.Create(
            context.ChoiceId,
            accepted: true,
            "La malédiction est refusée.",
            new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Toutes les portes ne méritent pas d'être ouvertes.")
            });
    }
}
