using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Effects;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Effects;

public sealed class RuntimeEffectResolver : IRuntimeEffectResolver
{
    public RuntimeEffectResolution Resolve(RuntimeEffectResolutionContext context)
    {
        var effect = context.Effect;
        var targets = context.Targets;
        var updatedTargets = new List<Combatant>();
        var createdModifiers = new List<RunModifier>();

        foreach (var target in targets)
        {
            if (effect.IsImmediate)
            {
                ApplyImmediateEffect(target, effect);
                updatedTargets.Add(target);
            }

            if (effect.IsPersistent)
            {
                var modifier = CreateModifier(target, effect, context);
                createdModifiers.Add(modifier);
            }
        }

        return new RuntimeEffectResolution(
            updatedTargets.AsReadOnly(),
            createdModifiers.AsReadOnly());
    }

    private static void ApplyImmediateEffect(Combatant target, RuntimeEffect effect)
    {
        var baseValue = GetBaseValueForEffect(target, effect);
        var resolvedValue = (int)effect.ResolveValue(baseValue);

        switch (effect.EffectType)
        {
            case Domain.Effects.EffectType.HealVitality:
                if (resolvedValue <= 0)
                    throw new DomainException("HealVitality requires a positive value.");
                target.ApplyHeal(resolvedValue);
                break;

            case Domain.Effects.EffectType.DamageVitality:
                if (resolvedValue <= 0)
                    throw new DomainException("DamageVitality requires a positive value.");
                target.ApplyDamage(resolvedValue);
                break;

            case Domain.Effects.EffectType.AddCurrentGuard:
                if (resolvedValue <= 0)
                    throw new DomainException("AddCurrentGuard requires a positive value.");
                target.GainGuard(resolvedValue);
                break;

            case Domain.Effects.EffectType.RestoreFocus:
            case Domain.Effects.EffectType.RestoreMana:
            case Domain.Effects.EffectType.RestoreCharge:
                break;

            default:
                throw new DomainException(
                    $"Immediate effect type '{effect.EffectType}' is not supported by the resolver.");
        }
    }

    private static decimal GetBaseValueForEffect(Combatant target, RuntimeEffect effect)
    {
        if (effect.ValueMode is Domain.Effects.ValueMode.Flat or Domain.Effects.ValueMode.TagOnly)
            return 0;

        return effect.EffectType switch
        {
            Domain.Effects.EffectType.HealVitality => target.MaxVitality,
            Domain.Effects.EffectType.DamageVitality => target.CurrentVitality,
            Domain.Effects.EffectType.AddCurrentGuard => target.Guard,
            _ => 0,
        };
    }

    private static RunModifier CreateModifier(Combatant target, RuntimeEffect effect, RuntimeEffectResolutionContext context)
    {
        var modifierType = MapEffectTypeToModifierType(effect.EffectType);
        var duration = MapEffectDuration(effect.Duration);
        var baseValue = GetBaseValueForModifier(target, effect);
        var value = (double)effect.ResolveValue(baseValue);
        var effectiveValue = effect.EffectType switch
        {
            Domain.Effects.EffectType.AddStartingGuard => (int)Math.Round(value),
            Domain.Effects.EffectType.ModifyDifficultyMultiplier => value,
            Domain.Effects.EffectType.ModifyRewardPowerMultiplier => value,
            _ => value,
        };
        var stackPolicy = effect.StackPolicy.ToString();

        return RunModifier.Create(
            modifierType,
            effectiveValue,
            duration,
            context.SourceType,
            context.SourceKey,
            valueMode: effect.ValueMode.ToString(),
            stackPolicy: stackPolicy);
    }

    private static decimal GetBaseValueForModifier(Combatant target, RuntimeEffect effect)
    {
        if (effect.ValueMode is Domain.Effects.ValueMode.Flat or Domain.Effects.ValueMode.TagOnly)
            return 0;

        return effect.EffectType switch
        {
            Domain.Effects.EffectType.AddStartingGuard => target.MaxVitality,
            Domain.Effects.EffectType.ModifyDifficultyMultiplier => 1m,
            Domain.Effects.EffectType.ModifyRewardPowerMultiplier => 1m,
            _ => 0,
        };
    }

    private static RunModifierType MapEffectTypeToModifierType(EffectType effectType)
    {
        return effectType switch
        {
            Domain.Effects.EffectType.AddStartingGuard => RunModifierType.StartingGuardBonus,
            Domain.Effects.EffectType.ModifyDifficultyMultiplier => RunModifierType.CombatDifficultyMultiplier,
            Domain.Effects.EffectType.ModifyRewardPowerMultiplier => RunModifierType.RewardPowerMultiplier,
            Domain.Effects.EffectType.ModifyAttackPower => RunModifierType.AttackPowerBonus,
            Domain.Effects.EffectType.ModifyDefense => RunModifierType.DefenseBonus,
            Domain.Effects.EffectType.ModifySpeed => RunModifierType.SpeedBonus,
            Domain.Effects.EffectType.ModifyInitiative => RunModifierType.InitiativeBonus,
            Domain.Effects.EffectType.ModifyRecovery => RunModifierType.RecoveryBonus,
            Domain.Effects.EffectType.RestoreFocus => RunModifierType.FocusBonus,
            _ => throw new DomainException(
                $"Cannot map effect type '{effectType}' to a RunModifierType."),
        };
    }

    private static RunModifierDuration MapEffectDuration(EffectDuration duration)
    {
        return duration switch
        {
            Domain.Effects.EffectDuration.Immediate => RunModifierDuration.Immediate,
            Domain.Effects.EffectDuration.CurrentCombat => RunModifierDuration.NextCombatOnly,
            Domain.Effects.EffectDuration.NextCombatOnly => RunModifierDuration.NextCombatOnly,
            Domain.Effects.EffectDuration.NextRewardOnly => RunModifierDuration.NextRewardOnly,
            Domain.Effects.EffectDuration.UntilRoomEnds => RunModifierDuration.UntilRoomEnds,
            Domain.Effects.EffectDuration.UntilRunEnds => RunModifierDuration.UntilRunEnds,
            Domain.Effects.EffectDuration.PermanentCandidate => RunModifierDuration.PermanentCandidate,
            _ => RunModifierDuration.UntilRunEnds,
        };
    }
}
