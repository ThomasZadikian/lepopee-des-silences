using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Effects;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.PalaceLaws;

/// <summary>
/// Shared <see cref="PalaceLawDefinitionSnapshot"/> → <see cref="PalaceLaw"/> construction —
/// previously duplicated across the (now-retired) Law node choice resolver, NpcConsequenceApplier,
/// and DevToolsRunDebugService. Threads the promulgation metadata (rarity/polarity/majeure/
/// roomKey/isCumulExempt/exclusionKeys) the ambient promulgation engine relies on.
/// </summary>
public static class PalaceLawMapper
{
    public static PalaceLaw CreatePalaceLaw(
        PalaceLawDefinitionSnapshot definition,
        PalaceLawDomain fallbackDomain = PalaceLawDomain.Narrative)
    {
        var domains = definition.ImpactDomains
            .Select(domain => Enum.TryParse<PalaceLawDomain>(domain, ignoreCase: true, out var parsed)
                ? parsed
                : (PalaceLawDomain?)null)
            .Where(domain => domain.HasValue)
            .Select(domain => domain!.Value)
            .Distinct()
            .ToArray();

        if (domains.Length == 0)
        {
            domains = [fallbackDomain];
        }

        var effects = (definition.Effects ?? [])
            .OrderBy(effect => effect.Order)
            .Select(MapEffect)
            .Where(effect => effect is not null)
            .Select(effect => effect!)
            .ToArray();

        return PalaceLaw.Create(
            definition.Key,
            definition.Name,
            definition.Version,
            domains,
            effects,
            rarity: definition.Rarity,
            polarity: definition.Polarity,
            isMajeure: definition.IsMajeure,
            roomKey: definition.RoomKey,
            isCumulExempt: definition.IsCumulExempt,
            exclusionKeys: definition.ExclusionKeys);
    }

    private static PalaceLawEffect? MapEffect(CatalogEffectDefinitionSnapshot effect)
    {
        if (!Enum.TryParse<EffectType>(effect.EffectType, ignoreCase: true, out var effectType))
        {
            throw new DomainException($"Palace law effect type '{effect.EffectType}' is not supported by the runtime.");
        }

        var duration = Enum.TryParse<RunModifierDuration>(effect.Duration, ignoreCase: true, out var parsedDuration)
            ? parsedDuration
            : RunModifierDuration.UntilRunEnds;

        return effectType switch
        {
            EffectType.AddStartingGuard => PalaceLawEffect.Create(
                RunModifierType.StartingGuardBonus, (double)effect.Value, duration),
            EffectType.ModifyDifficultyMultiplier => PalaceLawEffect.Create(
                RunModifierType.CombatDifficultyMultiplier, (double)effect.Value, duration),
            EffectType.ModifyRewardPowerMultiplier => PalaceLawEffect.Create(
                RunModifierType.RewardPowerMultiplier, (double)effect.Value, duration),
            EffectType.ModifyAttackPower => PalaceLawEffect.Create(
                RunModifierType.AttackPowerBonus, (double)effect.Value, duration),
            EffectType.ModifyDefense => PalaceLawEffect.Create(
                RunModifierType.DefenseBonus, (double)effect.Value, duration),
            EffectType.ModifySpeed => PalaceLawEffect.Create(
                RunModifierType.SpeedBonus, (double)effect.Value, duration),
            EffectType.ApplyRoomClimate => PalaceLawEffect.Create(
                RunModifierType.RoomClimate,
                MapClimate(effect.Condition ?? effect.BehaviorTag),
                RunModifierDuration.UntilRoomEnds),
            EffectType.ModifyGenerationWeight => null,
            EffectType.ModifyEnemyBehavior => null,

            // Lois du Palais — gate-style effects (see EffectType.cs for the law → mechanic map).
            EffectType.EnableTurnOrderReverse => PalaceLawEffect.Create(
                RunModifierType.TurnOrderReverse, 1, duration),
            EffectType.EnableTurnOrderLock => PalaceLawEffect.Create(
                RunModifierType.TurnOrderLock, 1, duration),
            EffectType.EnableRoomTraversalHpDrain => PalaceLawEffect.Create(
                RunModifierType.RoomTraversalHpDrain, 1, duration),
            EffectType.EnableHitCounterDoubleDamage => PalaceLawEffect.Create(
                RunModifierType.HitCounterDoubleDamage, 1, duration),
            EffectType.EnableMirrorCombatCopy => PalaceLawEffect.Create(
                RunModifierType.MirrorCombatCopy, 1, duration),
            EffectType.EnableFirstHitCritical => PalaceLawEffect.Create(
                RunModifierType.FirstHitCritical, 1, duration),
            EffectType.EnableLowHpDamageAmplification => PalaceLawEffect.Create(
                RunModifierType.DamageAmplificationBelowHpThreshold, 1, duration),
            EffectType.EnableDotDurationExtension => PalaceLawEffect.Create(
                RunModifierType.DotDurationExtension, (double)effect.Value, duration),
            EffectType.EnableDuelDamageAsymmetry => PalaceLawEffect.Create(
                RunModifierType.DuelDamageAsymmetry, 1, duration),
            EffectType.EnableCruelDestinyForEveryone => PalaceLawEffect.Create(
                RunModifierType.CruelDestinyForEveryone, 1, duration),
            EffectType.EnableAllyHealingBonus => PalaceLawEffect.Create(
                RunModifierType.AllyHealingBonus, (double)effect.Value, duration),
            EffectType.EnableReputationChangeDoubled => PalaceLawEffect.Create(
                RunModifierType.ReputationChangeDoubled, 1, duration),

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
            // Chapitre II — the Compendium's actual 5 canonical climates (see
            // CombatFactory.RoomClimate for why these are separate from the 4 above).
            "brume" or "voile" => 5,
            "orage" or "accords" => 6,
            "pluie-de-cendres" or "pluie de cendres" or "deuil-sec" => 7,
            "pluie-violacee" or "pluie violacee" or "pluie-violacée" or "pluie violacée" or "maree-haute" or "marée-haute" => 8,
            _ => throw new DomainException("ApplyRoomClimate requires a supported climate condition.")
        };
    }
}
