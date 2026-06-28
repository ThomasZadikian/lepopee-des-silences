using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Effects;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Events;

/// <summary>
/// Applies NPC reward/curse draw results to the run by reusing the existing law/curse
/// construction rules (kept isolated from the Law/Curse choice resolvers).
/// </summary>
public static class NpcConsequenceApplier
{
    public static void ApplyDamage(Run run, int amount)
    {
        if (amount > 0)
        {
            run.ApplyVitalityLoss(amount);
        }
    }

    public static void ApplyCurse(Run run, CatalogCurseDefinitionSnapshot definition)
    {
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

        run.ApplyCurse(curse);

        run.AddRunModifier(RunModifier.Create(
            RunModifierType.NextCombatDifficultyMultiplier,
            difficultyDelta,
            RunModifierDuration.NextCombatOnly,
            "Curse",
            definition.Key));
    }

    public static void ApplyLaw(Run run, PalaceLawDefinitionSnapshot definition)
    {
        run.ActivatePalaceLaw(CreatePalaceLaw(definition));
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
            EffectType.AddStartingGuard => PalaceLawEffect.Create(RunModifierType.StartingGuardBonus, (double)effect.Value, duration),
            EffectType.ModifyDifficultyMultiplier => PalaceLawEffect.Create(RunModifierType.CombatDifficultyMultiplier, (double)effect.Value, duration),
            EffectType.ModifyRewardPowerMultiplier => PalaceLawEffect.Create(RunModifierType.RewardPowerMultiplier, (double)effect.Value, duration),
            EffectType.ModifyAttackPower => PalaceLawEffect.Create(RunModifierType.AttackPowerBonus, (double)effect.Value, duration),
            EffectType.ModifyDefense => PalaceLawEffect.Create(RunModifierType.DefenseBonus, (double)effect.Value, duration),
            EffectType.ModifySpeed => PalaceLawEffect.Create(RunModifierType.SpeedBonus, (double)effect.Value, duration),
            EffectType.ApplyRoomClimate => PalaceLawEffect.Create(RunModifierType.RoomClimate, MapClimate(effect.Condition ?? effect.BehaviorTag), RunModifierDuration.UntilRoomEnds),
            EffectType.ModifyGenerationWeight => null,
            EffectType.ModifyEnemyBehavior => null,
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