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

        // Gates AppliesToActor status effects (e.g. a self-buff on hit) and, per-target,
        // which targets actually receive a Damage skill's attached status effects (DoT,
        // debuff...): only a Damage skill can actually miss, so every other effect type
        // "connects" unconditionally (hitTargetIds stays null — no per-target gating).
        var attackLanded = true;
        HashSet<Guid>? hitTargetIds = null;

        switch (ResolveEffectType(skill))
        {
            case "Damage":
                hitTargetIds = ResolveDamage(combat, actor, skill, targets, logEntries);
                attackLanded = hitTargetIds.Count > 0;
                break;

            case "Guard":
                ResolveGuard(actor, skill, targets, logEntries);
                break;

            case "Weaken":
                ResolveTextEffect(actor, skill, targets, "EffectApplied", "weakens", logEntries);
                break;

            case "Heal":
                // "Loi des Visites Terminées" (room.hopital): "les sorts de soin sont sans
                // effet dans cette salle" — item-based healing (a separate command path,
                // UseItemInCombatCommandHandler) is untouched.
                if (!combat.HealingBlocked)
                {
                    ResolveHeal(combat, actor, skill, targets, logEntries);
                }
                break;

            case "RestoreMana":
                ResolveRestoreMana(actor, skill, targets, logEntries);
                break;

            case "Disrupt":
                ResolveTextEffect(actor, skill, targets, "EffectApplied", "disrupts", logEntries);
                break;

            case "CopySkills":
                ResolveCopySkills(combat, actor, skill, targets, logEntries);
                break;

            case "ExtendDotDuration":
                ResolveExtendDotDuration(combat, actor, skill, targets, logEntries);
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
        ApplySkillStatus(combat, actor, skill, targets, logEntries, attackLanded, hitTargetIds);
        return new CombatSkillEffectResolution(logEntries, combat);
    }

    private static void ConsumeResources(Combatant actor, CombatantSkill skill)
    {
        if (actor.Side != CombatantSide.Player)
            return; // enemies cast freely

        // Mina's "Protection de Him'Lit" (-5%, permanent) is the only source of this
        // today — see Combatant.EffectiveSkillCostReductionPercent.
        var reductionPercent = actor.EffectiveSkillCostReductionPercent;
        // "Loi du Silence Dû" (law.silence-du): "les sorts coûtent +2 mana" — a flat bonus
        // added on top of the percentage reduction above, distinct unit (see
        // Combatant.EffectiveFlatManaCostBonus).
        var manaCost = Math.Max(0, ApplyCostReduction(skill.ManaCost, reductionPercent) + actor.EffectiveFlatManaCostBonus);
        var chargeCost = ApplyCostReduction(skill.ChargeCost, reductionPercent);

        actor.SpendMana(manaCost);
        actor.SpendCharge(chargeCost);
    }

    private static int ApplyCostReduction(int baseCost, int reductionPercent) =>
        reductionPercent == 0
            ? baseCost
            : Math.Max(0, (int)Math.Round(baseCost * (1.0 - reductionPercent / 100.0)));

    private static void ResolveHeal(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        List<CombatLogEntryDto> logEntries)
    {
        foreach (var target in targets)
        {
            if (target.IsDefeated || target.CurrentVitality >= target.MaxVitality)
                continue;

            var baseHealAmount = skill.BasePowerIsPercentOfMaxVitality
                ? (int)Math.Round(target.MaxVitality * (skill.BasePower / 100.0))
                : skill.BasePower;
            // Back row's -10% healing received malus — the strategic counterweight to
            // the row's other benefits (see Combatant.RowHealingReceivedReductionPercent).
            var healAmount = (int)Math.Round(baseHealAmount
                * (1.0 + actor.EffectiveHealingBonusPercent / 100.0)
                * (1.0 - target.RowHealingReceivedReductionPercent / 100.0));

            // "Loi de la Troisième Tasse": may halve healAmount and apply a light
            // poison to the target. No-op when the law is inactive.
            var (correctedHealAmount, thirdCupTriggered) = combat.ApplyThirdCupRollIfActive(target, healAmount);
            healAmount = correctedHealAmount;

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

                if (thirdCupTriggered)
                {
                    logEntries.Add(CreateLog(
                        "StatusApplied",
                        $"{target.DisplayName}'s cup was the third one — a light poison sets in.",
                        actor,
                        skill,
                        [target]));
                }
            }
        }
    }

    // "Recharge" (Encrier Vivant) : transfère du mana brut à un allié — pas de bonus/
    // malus de stat, contrairement à Heal (pas de ressource "MagicAttack de mana").
    private static void ResolveRestoreMana(
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        List<CombatLogEntryDto> logEntries)
    {
        foreach (var target in targets)
        {
            if (target.IsDefeated || target.Mana >= target.MaxMana)
                continue;

            var before = target.Mana;
            target.GainMana(skill.BasePower);
            var restored = target.Mana - before;

            if (restored > 0)
            {
                logEntries.Add(CreateLog(
                    "ManaRestored",
                    $"{target.DisplayName} recovers {restored} mana.",
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

    // "Écriture continuelle" (l'Écrivain, sort légendaire) : allonge de BasePower% le
    // nombre de ticks RESTANTS de tous les DamageOverTime actifs sur la cible — pas
    // la durée d'origine, et pas un nouveau stack (voir CombatStatusEffect.Reinforce :
    // re-appliquer un DoT n'allonge jamais sa durée ; seul ExtendDuration le fait).
    // BasePower encode ici un pourcentage (25 = +25%), même degré de liberté
    // contextuelle par EffectType que Heal ("% des PV max")/Création ("nb de tours").
    private static void ResolveExtendDotDuration(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        List<CombatLogEntryDto> logEntries)
    {
        foreach (var target in targets)
        {
            if (target.IsDefeated)
                continue;

            var extendedAny = false;
            foreach (var effect in target.StatusEffects.Where(e => e.Kind == StatusEffectKind.DamageOverTime))
            {
                var remaining = effect.ExpiresAtTick - combat.CurrentTick;
                if (remaining <= 0)
                    continue;

                var extendedRemaining = (int)Math.Round(remaining * (1.0 + Math.Max(0, skill.BasePower) / 100.0));
                effect.ExtendDuration(combat.CurrentTick + extendedRemaining);
                extendedAny = true;
            }

            if (extendedAny)
            {
                logEntries.Add(CreateLog(
                    "StatusApplied",
                    $"{target.DisplayName}'s afflictions linger longer.",
                    actor,
                    skill,
                    [target]));
            }
        }
    }

    private static void ApplySkillStatus(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        List<CombatLogEntryDto> logEntries,
        bool attackLanded,
        IReadOnlySet<Guid>? hitTargetIds)
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
                ApplyStatusEffectSpec(combat, skill, spec, actor, actor);

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

            // A Damage skill that also carries a DoT/debuff must actually connect with
            // THIS target before that status effect takes hold — a missed hit shouldn't
            // still poison. Non-Damage skills (pure status effects) have no miss roll,
            // so hitTargetIds is null there and every target is affected as before.
            if (hitTargetIds is not null && !hitTargetIds.Contains(target.Id.Value))
                continue;

            foreach (var spec in skill.StatusEffects.Where(s => !s.AppliesToActor))
            {
                ApplyStatusEffectSpec(combat, skill, spec, target, actor);

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

    private static void ApplyStatusEffectSpec(
        Combat combat, CombatantSkill skill, SkillStatusEffectSpec spec, Combatant recipient, Combatant caster)
    {
        // Equipment-driven DOT resistance (e.g. Main de Khasma) shortens the
        // duration of an incoming DamageOverTime effect; the per-tick damage
        // reduction itself is applied later, at tick time (Combatant.TickStatusEffects).
        var durationTicks = spec.Kind == StatusEffectKind.DamageOverTime && recipient.DotDurationReductionPercent > 0 && !spec.IsPermanent
            ? Math.Max(1, (int)Math.Round(
                spec.DurationTicks * (1.0 - Math.Min(recipient.DotDurationReductionPercent, 100) / 100.0)))
            : spec.DurationTicks;

        // "Loi de l'Écriture" (law.ecriture): "tous les DoT (des deux camps) durent
        // +2 tours" — a flat tick bonus on top of the equipment-driven reduction above,
        // symmetric across both sides.
        if (spec.Kind == StatusEffectKind.DamageOverTime && !spec.IsPermanent)
        {
            durationTicks += combat.DotDurationExtensionTicks;
        }

        // Flat-magnitude DoTs (poison/burn/ink...) are attack rolls like any other
        // damage: they scale with the caster's Attack/Defense (or MagicAttack/
        // MagicDefense for Magic-category skills) via the SAME symmetric ratio and
        // magic bonus/reduction multipliers instant Damage skills use — see
        // StatModifierDamageMultiplier/MagicCategoryDamageMultiplier. Percent-of-max
        // DoTs (e.g. "Une destinée cruelle", a self-inflicted curse proportional to
        // the caster's own max HP) are excluded: they aren't an attack against a
        // defender, so an attack/defense ratio has no meaning there.
        var statMultiplier = spec.Kind == StatusEffectKind.DamageOverTime && !spec.MagnitudeIsPercentOfMax
            ? StatModifierDamageMultiplier(skill, caster, recipient)
                * MagicCategoryDamageMultiplier(skill, caster, recipient)
                * PhysicalCategoryDamageMultiplier(skill, caster)
                * RowDamageMultiplier(skill, caster, recipient)
            : 1.0;

        // Equipment/skill-driven DOT damage bonus (e.g. l'Écrivain's Plume d'écrivain)
        // is baked into the per-tick magnitude at cast time, from the CASTER's stat —
        // distinct from the recipient-side DotDamageReductionPercent applied at tick time.
        var bonusMultiplier = spec.Kind == StatusEffectKind.DamageOverTime && caster.EffectiveDotDamageBonusPercent > 0
            ? 1.0 + caster.EffectiveDotDamageBonusPercent / 100.0
            : 1.0;

        var magnitude = spec.Kind == StatusEffectKind.DamageOverTime
            ? Math.Max(1, (int)Math.Round(spec.Magnitude * statMultiplier * bonusMultiplier, MidpointRounding.AwayFromZero))
            : spec.Magnitude;

        // "Loi de la Marée Haute" (law.maree-haute, Pluie violacée): "+1 dégât par tour à
        // tous les DoT" — a flat bonus on flat-magnitude DoTs only; percent-of-max DoTs
        // (e.g. "Une destinée cruelle") aren't comparable to a flat unit and are excluded.
        if (spec.Kind == StatusEffectKind.DamageOverTime && !spec.MagnitudeIsPercentOfMax && !spec.IsPermanent)
        {
            magnitude += combat.DotMagnitudeBonus;
        }

        recipient.ApplyStatusEffect(CombatStatusEffect.Create(
            key: spec.Key,
            displayName: spec.DisplayName,
            kind: spec.Kind,
            currentTick: combat.CurrentTick,
            durationTicks: durationTicks,
            magnitude: magnitude,
            stacks: spec.Stacks,
            tickInterval: spec.TickInterval,
            stat: spec.Stat,
            emotionalType: spec.EmotionalType,
            isMagnitudePercentOfMax: spec.MagnitudeIsPercentOfMax,
            isMagnitudePercentOfBaseStat: spec.MagnitudeIsPercentOfBaseStat,
            isPermanent: spec.IsPermanent));
    }

    /// <returns>The ids of every target actually struck (not missed) by this skill.</returns>
    private HashSet<Guid> ResolveDamage(
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
        var hitTargetIds = new HashSet<Guid>();

        foreach (var target in targets)
        {
            var hitChance = HitChanceCalibration.HitChanceFromBonus(
                actor.HitChanceBonusPercent - RowAccuracyPenaltyPercent(actor, target));
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

            hitTargetIds.Add(target.Id.Value);

            // "Loi du Treizième Coup": every 13th landed hit (all sides combined) deals
            // double damage. Registered here, right as the hit is confirmed to land.
            var isThirteenthHit = combat.RegisterLandedHit();

            // "Loi de la Première Impression": the combat's very first landed hit (any
            // side, whoever lands it) is forced to critical. Combat-scoped, not
            // combatant-scoped — consumed the moment ANY hit lands, at most once.
            var isFirstHitCritical = combat.TryConsumeFirstHitCritical();

            var defenderProfile = _typeProfileProvider.Resolve(target);
            var critRoll = DeterministicCombatRoll.UnitInterval(BuildCritSeed(combat, actor, target, skill));

            // Attack buffs (actor) and defense buffs (target) shift the hit before
            // type/crit are applied.
            var basePower = ApplyStatMultiplier(
                skill.BasePower,
                StatModifierDamageMultiplier(skill, actor, target)
                    * MagicCategoryDamageMultiplier(skill, actor, target)
                    * PhysicalCategoryDamageMultiplier(skill, actor)
                    * RowDamageMultiplier(skill, actor, target)
                    * DuelDamageAsymmetryMultiplier(combat, skill));

            var effectiveCritChance = isFirstHitCritical ? 1.0 : critChance;

            var outcome = DamageCalculator.Calculate(
                basePower,
                attackType,
                defenderProfile,
                effectiveCritChance,
                critRoll);

            outcome = ApplyEquipmentDamageReduction(outcome, target, attackType);

            if (isThirteenthHit && outcome.FinalAmount > 0)
            {
                outcome = outcome with { FinalAmount = outcome.FinalAmount * 2 };
                logEntries.Add(CreateLog(
                    "ThirteenthHit",
                    $"Le treizième coup frappe {target.DisplayName} — dégâts doublés !",
                    actor,
                    skill,
                    [target]));
            }

            // "Loi de la Curée": +15% damage taken while the TARGET is already below
            // 25% of its max vitality — checked against vitality as it stood BEFORE
            // this hit lands, symmetric across both sides.
            if (combat.LowHpDamageAmplificationEnabled && outcome.FinalAmount > 0
                && target.CurrentVitality * 100 < target.MaxVitality * Combat.LowHpDamageAmplificationThresholdPercent)
            {
                outcome = outcome with
                {
                    FinalAmount = (int)Math.Round(
                        outcome.FinalAmount * (1.0 + Combat.LowHpDamageAmplificationBonusPercent / 100.0))
                };
            }

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
                // "Loi de l'Éloge Funèbre": arms the next actor's basic-attack-only
                // restriction. No-op when the law isn't active.
                combat.RegisterCombatantDefeated();

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

        return hitTargetIds;
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

    // Magic-category skills use EffectiveMagicAttack/EffectiveMagicDefense instead of
    // the physical AttackPower/Defense pair — symmetric ratio, separate stat channel.
    // Every combatant predating the Bestiaire chantier has MagicAttack/MagicDefense at
    // 0/0 by default, which keeps this multiplier neutral (1.0) for them.
    private static double StatModifierDamageMultiplier(CombatantSkill skill, Combatant actor, Combatant target)
    {
        var isMagic = string.Equals(skill.Category, "Magic", StringComparison.OrdinalIgnoreCase);
        var attack = isMagic ? actor.EffectiveMagicAttack : actor.EffectiveAttackPower;
        var defense = isMagic ? target.EffectiveMagicDefense : target.EffectiveDefense;

        var multiplier = (AttackDefenseBaseline + attack) / (AttackDefenseBaseline + defense);

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

    /// <summary>"Loi du Silence Dû" (law.silence-du): "les attaques physiques infligent
    /// +8% de dégâts" — symmetric counterpart to <see cref="MagicCategoryDamageMultiplier"/>,
    /// boosting Physical-category skills only (no reduction side exists for this stat yet).</summary>
    private static double PhysicalCategoryDamageMultiplier(CombatantSkill skill, Combatant actor)
    {
        if (!string.Equals(skill.Category, "Physical", StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        return Math.Max(0.0, 1.0 + actor.EffectivePhysicalDamageBonusPercent / 100.0);
    }

    /// <summary>
    /// Row positioning's damage-side effects: the attacker's own -15% physical damage
    /// dealt malus while in the Back row, the target's -5% incoming physical damage
    /// reduction while in the Back row (both Physical-category only), and the "tir à
    /// l'aveugle" stacking malus (-5% damage) when the attacker is in Back row AND
    /// targets a Back-row combatant — confirmed by the player to apply to both
    /// Physical and Magic. In practice the Physical half of that stacking malus never
    /// fires: Physical skills can never target a Back-row combatant at all (see
    /// CombatTargetingRuleValidator's "hors de portée" rule) — kept as specified
    /// rather than special-cased away.
    /// </summary>
    private static double RowDamageMultiplier(CombatantSkill skill, Combatant actor, Combatant target)
    {
        var multiplier = 1.0;

        if (string.Equals(skill.Category, "Physical", StringComparison.OrdinalIgnoreCase))
        {
            multiplier *= 1.0 - actor.RowPhysicalDamageDealtReductionPercent / 100.0;
            multiplier *= 1.0 - target.RowIncomingPhysicalDamageReductionPercent / 100.0;
        }

        if (actor.Row == CombatRow.Back && target.Row == CombatRow.Back)
        {
            multiplier *= 0.95;
        }

        return Math.Max(0.0, multiplier);
    }

    /// <summary>
    /// Row positioning's accuracy-side effects: the attacker's own -5% accuracy malus
    /// on all attacks while in the Back row, plus an additional -5% "tir à l'aveugle"
    /// malus when targeting another Back-row combatant while also in the Back row.
    /// </summary>
    private static int RowAccuracyPenaltyPercent(Combatant actor, Combatant target)
    {
        var penalty = actor.RowAccuracyPenaltyPercent;

        if (actor.Row == CombatRow.Back && target.Row == CombatRow.Back)
        {
            penalty += 5;
        }

        return penalty;
    }

    /// <summary>"Loi du Duel" (law.duel): "les attaques et sorts mono-cibles infligent
    /// +20% ; les attaques et sorts de zone infligent -20%", both sides. Mono-target =
    /// SingleEnemy/SingleAlly; area-of-effect = AllEnemies/AllAllies. Any other
    /// targeting type (e.g. Self) is neither and stays at 1.0.</summary>
    private static double DuelDamageAsymmetryMultiplier(Combat combat, CombatantSkill skill)
    {
        if (!combat.DuelDamageAsymmetryEnabled)
            return 1.0;

        return skill.TargetingType switch
        {
            "SingleEnemy" or "SingleAlly" => 1.0 + Combat.DuelSingleTargetBonusPercent / 100.0,
            "AllEnemies" or "AllAllies" => 1.0 - Combat.DuelAreaOfEffectPenaltyPercent / 100.0,
            _ => 1.0
        };
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