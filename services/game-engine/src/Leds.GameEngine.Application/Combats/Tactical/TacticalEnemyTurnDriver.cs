using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.SharedBuildingBlocks.Time;

namespace Leds.GameEngine.Application.Combats.Tactical;

/// <summary>Ce qu'un enchaînement de tours ennemis a produit.</summary>
public sealed record TacticalEnemyTurnsResult(
    IReadOnlyList<CombatLogEntryDto> LogEntries,
    IReadOnlyList<TacticalCombatEventDto> Events);

public interface ITacticalEnemyTurnDriver
{
    /// <summary>
    /// Joue les tours ennemis tant que la main leur appartient, et s'arrête dès qu'un allié la
    /// reprend ou que le combat s'achève.
    /// </summary>
    TacticalEnemyTurnsResult PlayWhileEnemyHasInitiative(Domain.Combats.Tactical.TacticalCombat combat);
}

/// <summary>
/// Fait agir les ennemis.
/// </summary>
/// <remarks>
/// <para>
/// Extrait de la commande de fin de tour parce qu'il faut le même enchaînement à <b>deux</b>
/// moments : quand le joueur passe la main, et dès l'ouverture du combat si l'initiative revient
/// à l'adversaire. Sans le second, un combat où la créature la plus rapide ouvre restait bloqué
/// pour toujours — le joueur n'a pas la main, et rien ne la lui rend.
/// </para>
/// </remarks>
public sealed class TacticalEnemyTurnDriver : ITacticalEnemyTurnDriver
{
    private sealed record EnemySkillPlan(
        CombatantSkill Skill,
        GridPosition Target,
        IReadOnlyCollection<Combatant> Targets);

    /// <summary>
    /// Plafond de sécurité sur les tours enchaînés. Un combat réel n'en produit qu'une poignée ;
    /// ce garde-fou n'existe que pour qu'un état incohérent échoue franchement au lieu de
    /// boucler indéfiniment dans une requête HTTP.
    /// </summary>
    private const int MaxChainedEnemyTurns = 64;

    /// <summary>Seuil de PV pour déclencher un soin (50%).</summary>
    private const double HealThreshold = 0.5;

    /// <summary>Seuil de garde pour déclencher un buff (30% de la garde max).</summary>
    private const double GuardThreshold = 0.3;

    private readonly ICombatSkillEffectResolver _effectResolver;
    private readonly IClock _clock;

    public TacticalEnemyTurnDriver(ICombatSkillEffectResolver effectResolver, IClock clock)
    {
        _effectResolver = effectResolver;
        _clock = clock;
    }

    public TacticalEnemyTurnsResult PlayWhileEnemyHasInitiative(
        Domain.Combats.Tactical.TacticalCombat combat)
    {
        ArgumentNullException.ThrowIfNull(combat);

        var log = new List<CombatLogEntryDto>();
        var events = new List<TacticalCombatEventDto>();
        var guard = 0;

        while (combat.Status == CombatStatus.Active && IsEnemyTurn(combat))
        {
            if (++guard > MaxChainedEnemyTurns)
                throw new InvalidOperationException(
                    "Tactical enemy turns did not settle; aborting to avoid an endless loop.");

            log.AddRange(PlayOneTurn(combat, events));

            combat.FailIfAllAlliesDefeated();
            combat.CompleteIfAllEnemiesDefeated();

            if (combat.Status != CombatStatus.Active)
                break;

            combat.AdvanceToNextCombatant();
        }

        return new TacticalEnemyTurnsResult(log, events);
    }

    private static bool IsEnemyTurn(Domain.Combats.Tactical.TacticalCombat combat) =>
        combat.ActiveCombatantId is { } id && combat.Enemies.Any(e => e.Id.Value == id);

    /// <summary>
    /// Un tour ennemi : choisir une proie, marcher vers elle, frapper si elle est à portée.
    /// </summary>
    /// <remarks>
    /// L'ordre — cible, puis chemin — vient de <see cref="TacticalEnemyAi"/> : une créature hors
    /// de portée avance quand même plutôt que de gâcher son tour sur place.
    /// </remarks>
    private IReadOnlyCollection<CombatLogEntryDto> PlayOneTurn(
        Domain.Combats.Tactical.TacticalCombat combat,
        List<TacticalCombatEventDto> events)
    {
        var log = new List<CombatLogEntryDto>();

        var actorId = combat.ActiveCombatantId!.Value;
        var actor = combat.Enemies.First(e => e.Id.Value == actorId);

        var prey = TacticalEnemyAi.ChooseTarget(combat, actorId);
        if (prey is null)
            return log;

        var destination = TacticalEnemyAi.ChooseDestination(combat, actorId, prey);

        if (destination != combat.PositionOf(actorId))
        {
            var move = combat.MoveActiveCombatant(destination);
            events.Add(TacticalCombatEventDto.Move(actorId, actor.DisplayName, move.Path));

            log.Add(new CombatLogEntryDto(
                OccurredAtUtc: _clock.UtcNow.UtcDateTime,
                Type: "TacticalMove",
                Message: $"{actor.DisplayName} avance en ({destination.X}, {destination.Y}) "
                         + $"pour {move.Cost} de mouvement.",
                ActorId: actorId,
                SkillKey: null,
                TargetIds: []));
        }

        var action = ChooseAction(combat, actor, prey);
        if (action is null)
        {
            log.Add(new CombatLogEntryDto(
                OccurredAtUtc: _clock.UtcNow.UtcDateTime,
                Type: "EnemyTurnResolved",
                Message: $"{actor.DisplayName} avance, mais reste hors de portée.",
                ActorId: actorId,
                SkillKey: null,
                TargetIds: []));
            return log;
        }

        var before = Runs.TacticalCombat.TacticalImpactRecorder.Capture(action.Targets);
        var speedsBefore = combat.CaptureEffectiveSpeeds();
        combat.OrientToward(actorId, action.Target);
        var resolution = _effectResolver.Resolve(combat, actor, action.Skill, action.Targets);
        foreach (var affected in action.Targets)
        {
            var facingTarget = TacticalSkillProfile.For(action.Skill).AreaShape == TacticalAreaShape.Single
                ? combat.PositionOf(actorId)
                : action.Target;
            combat.OrientToward(affected.Id.Value, facingTarget);
        }
        var impacts = Runs.TacticalCombat.TacticalImpactRecorder.Diff(
            before, action.Targets, combat);

        var tactical = TacticalSkillProfile.For(action.Skill);
        if (tactical.OncePerCombat)
            combat.MarkOnceSkillUsed(action.Skill.Key);

        combat.MarkActiveCombatantActed(action.Skill);

        if (combat.HaveEffectiveSpeedsChanged(speedsBefore))
            combat.RecalculateInitiativeAfterSpeedChange();

        events.Add(TacticalCombatEventDto.Skill(
            actorId,
            actor.DisplayName,
            action.Skill.Key,
            action.Skill.DisplayName,
            action.Target,
            impacts));

        log.Add(new CombatLogEntryDto(
            OccurredAtUtc: _clock.UtcNow.UtcDateTime,
            Type: "ActionAccepted",
            Message: $"\u00ab {action.Skill.DisplayName} \u00bb lancé par {actor.DisplayName}.",
            ActorId: actorId,
            SkillKey: action.Skill.Key,
            TargetIds: [.. action.Targets.Select(t => t.Id.Value)]));

        log.AddRange(resolution.LogEntries);

        return log;
    }

    /// <summary>
    /// La compétence offensive ou utilitaire la plus adaptée que l'ennemi peut utiliser.
    /// Priorise les soins et protections nécessaires, puis l'action offensive la plus forte.
    /// Les zones et les camps passent par les mêmes règles que les actions du joueur.
    /// </summary>
    private EnemySkillPlan? ChooseAction(
        Domain.Combats.Tactical.TacticalCombat combat, Combatant actor, Combatant prey)
    {
        var offensivePlans = actor.Skills
            .Where(s => TacticalTargeting.IsHostile(s.TargetingType))
            .Select(s => BuildPlan(combat, actor, s, prey))
            .Where(p => p is not null)
            .Cast<EnemySkillPlan>()
            .ToList();

        var utility = ChooseBestUtilityPlan(combat, actor, offensivePlans.Count > 0);
        if (utility is not null)
            return utility;

        return offensivePlans
            .OrderByDescending(p => p.Skill.BasePower)
            .ThenBy(p => p.Skill.Key, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static EnemySkillPlan? BuildPlan(
        TacticalCombat combat,
        Combatant actor,
        CombatantSkill skill,
        Combatant intendedTarget)
    {
        var tactical = TacticalSkillProfile.For(skill);
        if (tactical.OncePerCombat && combat.HasUsedOnceSkill(skill.Key))
            return null;
        if (combat.RemainingCooldown(actor.Id.Value, skill.Key) > 0)
            return null;

        var origin = combat.PositionOf(actor.Id.Value);
        var intendedCenter = skill.TargetingType == "Self" || tactical.AreaShape == TacticalAreaShape.Map
            ? origin
            : combat.PositionOf(intendedTarget.Id.Value);

        if (!TacticalTargeting.IsInRange(
                combat.Battlefield,
                origin,
                intendedCenter,
                tactical.Range,
                tactical.RequiresLineOfSight))
            return null;

        var interceptor = tactical.RequiresLineOfSight
            && tactical.AreaShape != TacticalAreaShape.Map
                ? TacticalTargeting.FindInterceptor(combat, origin, intendedCenter)
                : null;
        var center = interceptor is null
            ? intendedCenter
            : combat.PositionOf(interceptor.Id.Value);
        var affectedCells = tactical.AreaShape == TacticalAreaShape.Map
            ? null
            : TacticalTargeting.CellsInArea(combat.Battlefield, center, tactical.AreaShape);
        var targets = TacticalTargeting.ResolveTargets(
                combat,
                affectedCells,
                actor.Side,
                TacticalTargeting.IsHostile(skill.TargetingType),
                tactical.AreaShape)
            .ToList();

        if (interceptor is not null && targets.All(t => t.Id != interceptor.Id))
            targets.Add(interceptor);

        return targets.Count == 0 ? null : new EnemySkillPlan(skill, center, targets);
    }

    /// <summary>
    /// Choisit la meilleure compétence utilitaire en fonction du contexte (O-009).
    /// </summary>
    private EnemySkillPlan? ChooseBestUtilityPlan(
        TacticalCombat combat,
        Combatant actor,
        bool hasOffensiveSkills)
    {
        var allies = combat.Enemies.Where(e => !e.IsDefeated).ToList();
        var utilitySkills = actor.Skills
            .Where(s => !TacticalTargeting.IsHostile(s.TargetingType))
            .ToList();

        var wounded = allies
            .Where(e => e.CurrentVitality <= e.MaxVitality * HealThreshold)
            .OrderBy(e => e.CurrentVitality / (double)e.MaxVitality)
            .ThenBy(e => e.Id.Value)
            .ToList();
        var healingPlan = utilitySkills
            .Where(s => s.SkillType == "Heal" || s.EffectType == "Heal")
            .SelectMany(s => wounded.Select(target => BuildPlan(combat, actor, s, target)))
            .FirstOrDefault(p => p is not null);
        if (healingPlan is not null)
            return healingPlan;

        var exposed = allies
            .Where(e => e.Guard <= e.MaxVitality * GuardThreshold)
            .OrderBy(e => e.Guard)
            .ThenBy(e => e.Id.Value)
            .ToList();
        var guardPlan = utilitySkills
            .Where(s => s.SkillType == "Guard" || s.EffectType == "Guard")
            .SelectMany(s => exposed.Select(target => BuildPlan(combat, actor, s, target)))
            .FirstOrDefault(p => p is not null);
        if (guardPlan is not null)
            return guardPlan;

        if (hasOffensiveSkills)
            return null;

        return utilitySkills
            .Select(s => BuildPlan(combat, actor, s, actor))
            .Where(p => p is not null)
            .Cast<EnemySkillPlan>()
            .OrderByDescending(p => p.Skill.BasePower)
            .ThenBy(p => p.Skill.Key, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
