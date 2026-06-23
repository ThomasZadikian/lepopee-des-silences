using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Typing;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Application.Combats.Effects;

public sealed class CombatSkillEffectResolver : ICombatSkillEffectResolver
{
    private readonly ICombatantTypeProfileProvider _typeProfileProvider;

    public CombatSkillEffectResolver(ICombatantTypeProfileProvider? typeProfileProvider = null)
    {
        _typeProfileProvider = typeProfileProvider ?? new EmotionalTypeProfileProvider();
    }

    public CombatSkillEffectResolution Resolve(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets)
    {
        if (targets.Count == 0)
        {
            throw new DomainException("At least one target is required to resolve a skill effect.");
        }

        var logEntries = new List<CombatLogEntryDto>();

        switch (ResolveEffectType(skill))
        {
            case "Damage":
                ResolveDamage(combat, actor, skill, targets, logEntries);
                break;

            case "Guard":
                ResolveGuard(actor, skill, targets, logEntries);
                break;

            case "Weaken":
                ResolveTextEffect(actor, skill, targets, "EffectApplied", "weakens", logEntries);
                break;

            case "Disrupt":
                ResolveTextEffect(actor, skill, targets, "EffectApplied", "disrupts", logEntries);
                break;

            default:
                throw new DomainException($"Unsupported skill effect type: {skill.EffectType}");
        }

        return new CombatSkillEffectResolution(true, logEntries, combat);
    }

    private void ResolveDamage(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        List<CombatLogEntryDto> logEntries)
    {
        var attackType = _typeProfileProvider.ResolveAttackType(actor, skill);
        var critChance = CriticalHitCalibration.CritChanceFromFocus(actor.BaseStatSnapshot.Focus);

        foreach (var target in targets)
        {
            var defenderProfile = _typeProfileProvider.Resolve(target);
            var critRoll = DeterministicCombatRoll.UnitInterval(BuildCritSeed(combat, actor, target, skill));

            var outcome = DamageCalculator.Calculate(
                skill.BasePower,
                attackType,
                defenderProfile,
                critChance,
                critRoll);

            logEntries.Add(CreateLog(
                "SkillUsed",
                $"{actor.DisplayName} uses {skill.DisplayName} on {target.DisplayName} for {outcome.FinalAmount} damage{DescribeOutcome(outcome)}.",
                actor,
                skill,
                [target]));

            if (outcome.IsCritical)
            {
                logEntries.Add(CreateLog(
                    "CriticalHit",
                    $"Critical hit on {target.DisplayName}!",
                    actor,
                    skill,
                    [target]));
            }

            AddEffectivenessLog(actor, skill, target, outcome.Effectiveness, logEntries);

            if (outcome.Effectiveness == DamageEffectiveness.Immune || outcome.FinalAmount <= 0)
            {
                // Immune or no damage: nothing to apply, but the encounter still logged the attempt.
                continue;
            }

            var guardBefore = target.Guard;
            var vitalityBefore = target.CurrentVitality;

            target.ApplyDamage(outcome.FinalAmount);

            var absorbed = guardBefore - target.Guard;
            var vitalityDamage = vitalityBefore - target.CurrentVitality;

            if (absorbed > 0)
            {
                logEntries.Add(CreateLog(
                    "DamageApplied",
                    $"{target.DisplayName}'s guard absorbs {absorbed} damage.",
                    actor,
                    skill,
                    [target]));
            }

            if (vitalityDamage > 0)
            {
                logEntries.Add(CreateLog(
                    "DamageApplied",
                    $"{target.DisplayName} takes {vitalityDamage} damage.",
                    actor,
                    skill,
                    [target]));
            }

            if (target.IsDefeated)
            {
                logEntries.Add(CreateLog(
                    "TargetDefeated",
                    $"{target.DisplayName} is defeated.",
                    actor,
                    skill,
                    [target]));
            }
        }
    }

    private static string DescribeOutcome(DamageOutcome outcome)
    {
        var parts = new List<string>();

        if (outcome.IsCritical)
        {
            parts.Add("critical");
        }

        switch (outcome.Effectiveness)
        {
            case DamageEffectiveness.Weak:
                parts.Add("weakness");
                break;
            case DamageEffectiveness.Resistant:
                parts.Add("resisted");
                break;
            case DamageEffectiveness.Immune:
                parts.Add("immune");
                break;
        }

        return parts.Count == 0 ? string.Empty : $" ({string.Join(", ", parts)})";
    }

    private static void AddEffectivenessLog(
        Combatant actor,
        CombatantSkill skill,
        Combatant target,
        DamageEffectiveness effectiveness,
        List<CombatLogEntryDto> logEntries)
    {
        var (type, message) = effectiveness switch
        {
            DamageEffectiveness.Weak => ("WeaknessHit", $"{target.DisplayName} is weak to this — damage amplified."),
            DamageEffectiveness.Resistant => ("ResistedHit", $"{target.DisplayName} resists — damage reduced."),
            DamageEffectiveness.Immune => ("ImmuneHit", $"{target.DisplayName} is immune — no damage."),
            _ => (string.Empty, string.Empty)
        };

        if (type.Length == 0)
        {
            return;
        }

        logEntries.Add(CreateLog(type, message, actor, skill, [target]));
    }

    private static string BuildCritSeed(Combat combat, Combatant actor, Combatant target, CombatantSkill skill)
    {
        return string.Join(
            '|',
            "crit",
            combat.Id.Value.ToString("N"),
            combat.TurnNumber.ToString(),
            actor.Id.Value.ToString("N"),
            target.Id.Value.ToString("N"),
            skill.Key);
    }

    private static void ResolveGuard(
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        List<CombatLogEntryDto> logEntries)
    {
        foreach (var target in targets)
        {
            target.GainGuard(skill.BasePower);

            logEntries.Add(CreateLog(
                "GuardGained",
                $"{target.DisplayName} gains {skill.BasePower} guard.",
                actor,
                skill,
                [target]));
        }
    }

    private static void ResolveTextEffect(
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        string type,
        string verb,
        List<CombatLogEntryDto> logEntries)
    {
        foreach (var target in targets)
        {
            logEntries.Add(CreateLog(
                type,
                $"{actor.DisplayName} {verb} {target.DisplayName}.",
                actor,
                skill,
                [target]));
        }
    }

    private static string ResolveEffectType(CombatantSkill skill)
    {
        if (string.Equals(skill.Key, "skill.basic.weaken", StringComparison.OrdinalIgnoreCase))
        {
            return "Weaken";
        }

        if (string.Equals(skill.Key, "skill.basic.disrupt", StringComparison.OrdinalIgnoreCase))
        {
            return "Disrupt";
        }

        if (string.Equals(skill.Key, "skill.basic.guard", StringComparison.OrdinalIgnoreCase))
        {
            return "Guard";
        }

        if (string.Equals(skill.EffectType, "AddCurrentGuard", StringComparison.OrdinalIgnoreCase))
        {
            return "Guard";
        }

        if (string.Equals(skill.EffectType, "DamageVitality", StringComparison.OrdinalIgnoreCase))
        {
            return "Damage";
        }

        return skill.EffectType;
    }

    private static CombatLogEntryDto CreateLog(
        string type,
        string message,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets)
    {
        return new CombatLogEntryDto(
            OccurredAtUtc: DateTime.UtcNow,
            Type: type,
            Message: message,
            ActorId: actor.Id.Value,
            SkillKey: skill.Key,
            TargetIds: targets.Select(t => t.Id.Value).ToArray());
    }
}