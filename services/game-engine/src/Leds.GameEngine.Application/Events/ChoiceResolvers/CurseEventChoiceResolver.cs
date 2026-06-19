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
    private readonly ICatalogContentGateway _catalogContentGateway;

    public CurseEventChoiceResolver(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    public NodeEventType EventType => NodeEventType.Curse;

    public async Task<CurrentEventChoiceResolutionResult> ResolveAsync(
        CurrentEventChoiceResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        return context.ChoiceId switch
        {
            "accept-curse" => await AcceptCurseAsync(context, cancellationToken),
            "reject-curse" => RejectCurse(context),
            _ => throw new DomainException(
                $"Choice '{context.ChoiceId}' is not valid for event type '{EventType}'.")
        };
    }

    private async Task<CurrentEventChoiceResolutionResult> AcceptCurseAsync(
        CurrentEventChoiceResolutionContext context,
        CancellationToken cancellationToken)
    {
        var definition = (await _catalogContentGateway.ListAvailableCurseDefinitionsAsync(cancellationToken))
            .OrderBy(curse => curse.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault()
            ?? throw new DomainException("No available curse definition was found in Catalog.");

        var difficultyDelta = Math.Clamp(definition.Severity / 100.0, 0.01, 0.5);

        var curse = ActiveCurse.Create(
            definition.Key,
            definition.DisplayName,
            definition.Description,
            difficultyDelta,
            DateTime.UtcNow,
            curseDefinitionKey: definition.Key,
            severity: definition.Severity,
            duration: definition.Duration,
            effectSetKey: definition.EffectSetKey);

        context.Run.ApplyCurse(curse);

        var curseModifier = RunModifier.Create(
            RunModifierType.NextCombatDifficultyMultiplier,
            difficultyDelta,
            RunModifierDuration.NextCombatOnly,
            "Curse",
            definition.Key);
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
