using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Effects;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Events.ChoiceResolvers;

public sealed class LawEventChoiceResolver : ICurrentEventChoiceResolver
{
    private readonly ICatalogContentGateway _catalogContentGateway;

    public LawEventChoiceResolver(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    public NodeEventType EventType => NodeEventType.Law;

    public async Task<CurrentEventChoiceResolutionResult> ResolveAsync(
        CurrentEventChoiceResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var (action, lawKey) = ParseChoiceId(context.ChoiceId);

        return action switch
        {
            "accept-law" => await AcceptLaw(context, lawKey, cancellationToken),
            "reject-law" => RejectLaw(context),
            _ => throw new DomainException(
                $"Choice '{context.ChoiceId}' is not valid for event type '{EventType}'.")
        };
    }

    private async Task<CurrentEventChoiceResolutionResult> AcceptLaw(
        CurrentEventChoiceResolutionContext context,
        string lawKey,
        CancellationToken cancellationToken)
    {
        var lawDefinitionResult = await _catalogContentGateway.GetPalaceLawDefinitionByKeyAsync(
            lawKey,
            cancellationToken);

        if (lawDefinitionResult.IsFailure)
        {
            throw new DomainException(
                $"Failed to retrieve palace law definition: {lawDefinitionResult.Error.Message}");
        }

        var law = CreatePalaceLaw(lawDefinitionResult.Value);

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

    private static (string Action, string LawKey) ParseChoiceId(string choiceId)
    {
        var parts = choiceId.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new DomainException(
                $"Choice '{choiceId}' is not valid for event type '{NodeEventType.Law}'.");
        }

        return (parts[0], parts[1]);
    }

    private static PalaceLaw CreatePalaceLaw(PalaceLawDefinitionSnapshot definition)
    {
        var domains = definition.ImpactDomains
            .Select(MapDomain)
            .Distinct()
            .ToArray();

        if (domains.Length == 0)
        {
            domains = [PalaceLawDomain.Narrative];
        }

        var effects = (definition.Effects ?? [])
            .OrderBy(effect => effect.Order)
            .Select(MapEffect)
            .Where(effect => effect is not null)
            .Cast<PalaceLawEffect>()
            .ToArray();

        return PalaceLaw.Create(
            definition.Key,
            definition.Name,
            definition.Version,
            domains,
            effects);
    }

    private static PalaceLawDomain MapDomain(string domain)
    {
        return Enum.TryParse<PalaceLawDomain>(domain, ignoreCase: true, out var parsed)
            ? parsed
            : PalaceLawDomain.Narrative;
    }

    private static PalaceLawEffect? MapEffect(CatalogEffectDefinitionSnapshot effect)
    {
        if (!Enum.TryParse<EffectType>(effect.EffectType, ignoreCase: true, out var effectType))
        {
            throw new DomainException($"Unsupported palace law effect type '{effect.EffectType}'.");
        }

        var duration = Enum.TryParse<RunModifierDuration>(effect.Duration, ignoreCase: true, out var parsedDuration)
            ? parsedDuration
            : RunModifierDuration.UntilRunEnds;

        return effectType switch
        {
            EffectType.AddStartingGuard => PalaceLawEffect.Create(
                RunModifierType.StartingGuardBonus,
                (double)effect.Value,
                duration),
            EffectType.ModifyDifficultyMultiplier => PalaceLawEffect.Create(
                RunModifierType.CombatDifficultyMultiplier,
                (double)effect.Value,
                duration),
            EffectType.ModifyRewardPowerMultiplier => PalaceLawEffect.Create(
                RunModifierType.RewardPowerMultiplier,
                (double)effect.Value,
                duration),
            EffectType.ModifyAttackPower => PalaceLawEffect.Create(
                RunModifierType.AttackPowerBonus,
                (double)effect.Value,
                duration),
            EffectType.ModifyDefense => PalaceLawEffect.Create(
                RunModifierType.DefenseBonus,
                (double)effect.Value,
                duration),
            EffectType.ModifySpeed => PalaceLawEffect.Create(
                RunModifierType.SpeedBonus,
                (double)effect.Value,
                duration),
            EffectType.ApplyRoomClimate => PalaceLawEffect.Create(
                RunModifierType.RoomClimate,
                MapClimate(effect.Condition ?? effect.BehaviorTag),
                RunModifierDuration.UntilRoomEnds),
            EffectType.ModifyGenerationWeight => null,
            _ => throw new DomainException($"Palace law effect type '{effect.EffectType}' is not supported by the runtime.")
        };
    }

    private static double MapClimate(string? climate)
    {
        return climate?.Trim().ToLowerInvariant() switch
        {
            "grey" or "grisaille" => 1,
            "rain" or "pluie" => 2,
            "heatwave" or "canicule" => 3,
            "hail" or "grele" or "grêle" => 4,
            _ => throw new DomainException("ApplyRoomClimate requires a supported climate condition.")
        };
    }
}
