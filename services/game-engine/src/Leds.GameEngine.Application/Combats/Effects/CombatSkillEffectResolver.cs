using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.EnemyTurns.Ai;
using Leds.GameEngine.Application.Combats.Typing;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Atb;
using Leds.GameEngine.Domain.Combats.StatusEffects;
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

        // Players pay mana/charge to cast (enemies cast freely); affordability was
        // already validated for player-side actors.
        ConsumeResources(actor, skill);

        // Gates AppliesToActor status effects (e.g. a self-buff on hit): only a
        // Damage skill can actually miss, so every other effect type "connects"
        // unconditionally.
        var attackLanded = true;

        switch (ResolveEffectType(skill))
        {
            case "Damage":
                attackLanded = ResolveDamage(combat, actor, skill, targets, logEntries);
                break;

            case "Guard":
                ResolveGuard(actor, skill, targets, logEntries);
                break;

            case "Weaken":
                ResolveTextEffect(actor, skill, targets, "EffectApplied", "weakens", logEntries);
                break;

            case "Heal":
                ResolveHeal(actor, skill, targets, logEntries);
                break;

            case "Disrupt":
                ResolveTextEffect(actor, skill, targets, "EffectApplied", "disrupts", logEntries);
                break;

            case "CopySkills":
                ResolveCopySkills(combat, actor, skill, targets, logEntries);
                break;

            default:
                // Status-only spell (pure buff/debuff/control): no instant effect —
                // the durable status(es) below do the work.
                if (skill.StatusEffects.Count == 0)
                    throw new DomainException(
                        $"Unsupported skill effect type: {skill.EffectType} (skill '{skill.Key}'). " +
                        "A skill with a non-instant EffectType (e.g. Debuff/Buff/Status) must have an " +
                        "effect set in the catalog so it resolves to a durable status effect.");
                break;
        }

        // Apply the durable status(es) (poison/regen/buff/control) on top, if declared.
        ApplySkillStatus(combat, actor, skill, targets, logEntries, attackLanded);
        return new CombatSkillEffectResolution(true, logEntries, combat);
    }

    private static void ConsumeResources(Combatant actor, CombatantSkill skill)
    {
        if (actor.Side != CombatantSide.Player)
            return; // enemies cast freely

        actor.SpendMana(skill.ManaCost);
        actor.SpendCharge(skill.ChargeCost);
    }

    private static void ResolveHeal(
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        List<CombatLogEntryDto> logEntries)
    {
        foreach (var target in targets)
        {
            if (target.IsDefeated || target.CurrentVitality >= target.MaxVitality)
                continue;

            var healAmount = skill.BasePowerIsPercentOfMaxVitality
                ? (int)Math.Round(target.MaxVitality * (skill.BasePower / 100.0))
                : skill.BasePower;

            var before = target.CurrentVitality;
            target.ApplyHeal(healAmount);
            var healed = target.CurrentVitality - before;

            if (healed > 0)
            {
                if (actor.Side == CombatantSide.Player)
                    actor.AccrueThreat(healed * ThreatTuning.ThreatPerHealing);

                logEntries.Add(CreateLog(
                    "HealApplied",
                    $"{target.DisplayName} recovers {healed} vitality.",
                    actor,
                    skill,
                    [target]));
            }
        }
    }

    // "Création" (sort légendaire de l'Architecte) : le lanceur duplique temporairement
    // les sorts de sa cible. BasePower encode ici le nombre de TOURS (et non une
    // puissance) — même degré de liberté contextuelle par EffectType que Heal/Guard
    // ailleurs dans ce résolveur. Limite connue : seuls les sorts eux-mêmes sont
    // copiés, pas les StatusEffects qu'ils portent (ex. un debuff attaché à un sort
    // copié ne s'appliquerait pas) — non modélisé pour l'instant.
    private static void ResolveCopySkills(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        List<CombatLogEntryDto> logEntries)
    {
        var durationTicks = AtbConstants.TicksPerTurn * Math.Max(1, skill.BasePower);

        foreach (var target in targets)
        {
            if (target.IsDefeated || target.Id == actor.Id || target.Skills.Count == 0)
                continue;

            actor.ApplyStatusEffect(CombatStatusEffect.Create(
                key: $"creation:{target.Id.Value:N}",
                displayName: skill.DisplayName,
                kind: StatusEffectKind.SkillGrant,
                currentTick: combat.CurrentTick,
                durationTicks: durationTicks,
                grantedSkills: target.Skills));

            logEntries.Add(CreateLog(
                "StatusApplied",
                $"{actor.DisplayName} duplicates {target.DisplayName}'s skills.",
                actor,
                skill,
                [target]));
        }
    }

    private static void ApplySkillStatus(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        List<CombatLogEntryDto> logEntries,
        bool attackLanded)
    {
        if (skill.StatusEffects.Count == 0)
            return;

        // Effects flagged AppliesToActor land on the CASTER instead of the skill's
        // targets — e.g. a damage skill that also buffs itself on hit (La liberté
        // retrouvée: +10% Speed on the caster when it strikes an enemy). Gated on
        // attackLanded so a miss grants nothing.
        if (!actor.IsDefeated && attackLanded)
        {
            foreach (var spec in skill.StatusEffects.Where(s => s.AppliesToActor))
            {
                ApplyStatusEffectSpec(combat, spec, actor);

                logEntries.Add(CreateLog(
                    "StatusApplied",
                    $"{actor.DisplayName} gains {spec.DisplayName}.",
                    actor,
                    skill,
                    [actor]));
            }
        }

        foreach (var target in targets)
        {
            if (target.IsDefeated)
                continue;

            foreach (var spec in skill.StatusEffects.Where(s => !s.AppliesToActor))
            {
                ApplyStatusEffectSpec(combat, spec, target);

                logEntries.Add(CreateLog(
                    "StatusApplied",
                    $"{target.DisplayName} is afflicted by {spec.DisplayName}.",
                    actor,
                    skill,
                    [target]));

                if (target.Side != actor.Side)
                {
                    // Momentum: landing a debuff on an opponent earns a tempo boost.
                    actor.GainTempoMomentum(TempoMomentumCalibration.DebuffAppliedGainPerMille);
                }
            }
        }
    }

    private static void ApplyStatusEffectSpec(Combat combat, SkillStatusEffectSpec spec, Combatant recipient)
    {
        // Equipment-driven DOT resistance (e.g. Main de Khasma) shortens the
        // duration of an incoming DamageOverTime effect; the per-tick damage
        // reduction itself is applied later, at tick time (Combatant.TickStatusEffects).
        var durationTicks = spec.Kind == StatusEffectKind.DamageOverTime && recipient.DotDurationReductionPercent > 0
            ? Math.Max(1, (int)Math.Round(
                spec.DurationTicks * (1.0 - Math.Min(recipient.DotDurationReductionPercent, 100) / 100.0)))
            : spec.DurationTicks;

        recipient.ApplyStatusEffect(CombatStatusEffect.Create(
            key: spec.Key,
            displayName: spec.DisplayName,
            kind: spec.Kind,
            currentTick: combat.CurrentTick,
            durationTicks: durationTicks,
            magnitude: spec.Magnitude,
            stacks: spec.Stacks,
            tickInterval: spec.TickInterval,
            stat: spec.Stat,
            emotionalType: spec.EmotionalType,
            isMagnitudePercentOfMax: spec.MagnitudeIsPercentOfMax,
            isMagnitudePercentOfBaseStat: spec.MagnitudeIsPercentOfBaseStat));
    }

    /// <returns>True if at least one target was actually struck (not missed).</returns>
    private bool ResolveDamage(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        List<CombatLogEntryDto> logEntries)
    {
        var attackType = _typeProfileProvider.ResolveAttackType(actor, skill);
        // Effective Focus = base + active Focus buffs/debuffs; equipment (e.g. Iris's
        // Doudou de Ethan: +5%) adds a flat bonus on top, still capped by MaxCritChance.
        var critChance = Math.Min(
            CriticalHitCalibration.MaxCritChance,
            CriticalHitCalibration.CritChanceFromFocus(actor.EffectiveFocus) + actor.EffectiveCriticalChanceBonusPercent / 100.0);
        var staggers = IsStaggerSkill(skill);
        var anyHit = false;

        foreach (var target in targets)
        {
            var hitChance = HitChanceCalibration.HitChanceFromBonus(actor.HitChanceBonusPercent);
            var hitRoll = DeterministicCombatRoll.UnitInterval(BuildHitSeed(combat, actor, target, skill));

            if (hitRoll >= hitChance)
            {
                logEntries.Add(CreateLog(
                    "AttackMissed",
                    $"{actor.DisplayName}'s {skill.DisplayName} misses {target.DisplayName}.",
                    actor,
                    skill,
                    [target]));
                continue;
            }

            anyHit = true;

            var defenderProfile = _typeProfileProvider.Resolve(target);
            var critRoll = DeterministicCombatRoll.UnitInterval(BuildCritSeed(combat, actor, target, skill));

            // Attack buffs (actor) and defense buffs (target) shift the hit before
            // type/crit are applied.
            var basePower = ApplyStatMultiplier(
                skill.BasePower,
                StatModifierDamageMultiplier(actor, target) * MagicCategoryDamageMultiplier(skill, actor, target));

            var outcome = DamageCalculator.Calculate(
                basePower,
                attackType,
                defenderProfile,
                critChance,
                critRoll);

            outcome = ApplyEquipmentDamageReduction(outcome, target, attackType);

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

                // Momentum: an aggressive, impactful hit earns the actor a faster follow-up.
                actor.GainTempoMomentum(TempoMomentumCalibration.CriticalHitGainPerMille);
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

            target.RecordLastAttacker(actor.Id.Value);

            if (actor.Side == CombatantSide.Player)
            {
                actor.AccrueThreat(
                    vitalityDamage * ThreatTuning.ThreatPerVitalityDamage
                    + absorbed * ThreatTuning.ThreatPerGuardAbsorbed);
            }

            if (absorbed > 0)
            {
                logEntries.Add(CreateLog(
                    "DamageApplied",
                    $"{target.DisplayName}'s guard absorbs {absorbed} damage.",
                    actor,
                    skill,
                    [target]));

                if (guardBefore > 0 && target.Guard == 0)
                {
                    // Momentum: breaking through a target's guard entirely earns a tempo boost.
                    actor.GainTempoMomentum(TempoMomentumCalibration.GuardBreakGainPerMille);
                }
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
            else if (staggers)
            {
                // Interruption: push the target's ATB gauge back (bigger if it was charging).
                combat.ApplyAtbInterruption(target.Id.Value);
                logEntries.Add(CreateLog(
                    "AtbStagger",
                    $"{target.DisplayName}'s momentum is broken.",
                    actor,
                    skill,
                    [target]));
            }
        }

        return anyHit;
    }

    // Equipment-driven typed damage reduction (e.g. Craie créatrice: -15% Mémoire),
    // independent of the categorical weak/resist/immune type system already applied
    // by DamageCalculator above — a flat percentage taken off whatever it produced.
    private static DamageOutcome ApplyEquipmentDamageReduction(
        DamageOutcome outcome, Combatant target, EmotionalType attackType)
    {
        if (outcome.FinalAmount <= 0
            || !target.TypedDamageReductionPercent.TryGetValue(attackType, out var percent)
            || percent <= 0)
        {
            return outcome;
        }

        var reduced = (int)Math.Round(outcome.FinalAmount * (1.0 - Math.Min(percent, 100) / 100.0));
        return outcome with { FinalAmount = Math.Max(0, reduced) };
    }

    private static bool IsStaggerSkill(CombatantSkill skill)
    {
        return skill.Tags is { Count: > 0 }
            && skill.Tags.Any(tag => string.Equals(tag?.Trim(), "stagger", StringComparison.OrdinalIgnoreCase));
    }

    // Baseline added to both sides of the ratio (tunable). Keeps the multiplier
    // well-behaved when either stat is 0 (no authored stat block yet), while
    // making the ABSOLUTE Attack/Defense values matter, not just buff/debuff
    // deltas — two combatants both sitting at the baseline produce a neutral
    // 1.0 multiplier, same as the old delta-only formula's no-buff case.
    private const double AttackDefenseBaseline = 20.0;

    private static double StatModifierDamageMultiplier(Combatant actor, Combatant target)
    {
        var multiplier = (AttackDefenseBaseline + actor.EffectiveAttackPower)
            / (AttackDefenseBaseline + target.EffectiveDefense);

        return Math.Clamp(multiplier, 0.25, 3.0);
    }

    // Magic-category skills (e.g. Pomenian's "Connaissance académique") are boosted
    // by the caster's magic damage bonus and mitigated by the target's magic damage
    // reduction; Physical-category skills are untouched by either.
    private static double MagicCategoryDamageMultiplier(CombatantSkill skill, Combatant actor, Combatant target)
    {
        if (!string.Equals(skill.Category, "Magic", StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        var bonus = 1.0 + actor.EffectiveMagicDamageBonusPercent / 100.0;
        var reduction = 1.0 - Math.Min(target.EffectiveMagicDamageReductionPercent, 100) / 100.0;
        return Math.Max(0.0, bonus * reduction);
    }

    private static int ApplyStatMultiplier(int basePower, double statMultiplier)
    {
        if (basePower <= 0 || statMultiplier == 1.0)
        {
            return basePower;
        }

        return Math.Max(1, (int)Math.Round(basePower * statMultiplier, MidpointRounding.AwayFromZero));
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

    private static string BuildHitSeed(Combat combat, Combatant actor, Combatant target, CombatantSkill skill)
    {
        return string.Join(
            '|',
            "hit",
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

            if (actor.Side == CombatantSide.Player)
                actor.AccrueThreat(skill.BasePower * ThreatTuning.ThreatPerGuardGranted);

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

        if (string.Equals(skill.EffectType, "Heal", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(skill.EffectType, "RestoreVitality", StringComparison.OrdinalIgnoreCase))
        {
            return "Heal";
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