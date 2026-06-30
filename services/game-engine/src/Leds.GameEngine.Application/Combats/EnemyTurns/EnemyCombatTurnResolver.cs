using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.EnemyTurns.Ai;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Atb;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats.EnemyTurns;

public sealed class EnemyCombatTurnResolver : IEnemyCombatTurnResolver
{
    private readonly ICombatSkillActionValidator _actionValidator;
    private readonly ICombatSkillEffectResolver _effectResolver;
    private readonly IReadOnlyDictionary<string, IBossBehavior> _bossBehaviors;
    private readonly IEnemyActionPlanner _planner;

    public EnemyCombatTurnResolver(
        ICombatSkillActionValidator actionValidator,
        ICombatSkillEffectResolver effectResolver,
        IEnumerable<IBossBehavior>? bossBehaviors = null,
        IEnemyActionPlanner? planner = null)
    {
        _actionValidator = actionValidator;
        _effectResolver = effectResolver;
        _planner = planner ?? new UtilityEnemyActionPlanner();
        _bossBehaviors = (bossBehaviors ?? Enumerable.Empty<IBossBehavior>())
            .GroupBy(behavior => behavior.BossKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public EnemyCombatTurnResolution Resolve(Combat combat)
    {
        var logEntries = new List<CombatLogEntryDto>();

        if (combat.Status != CombatStatus.Active)
        {
            return NotResolved(combat, logEntries);
        }

        var actor = combat.GetActiveCombatant();

        if (actor.Side != CombatantSide.Enemy)
        {
            return NotResolved(combat, logEntries);
        }

        if (actor.IsDefeated)
        {
            combat.ElectActiveByReadiness();
            var nextAfterDefeated = combat.GetActiveCombatant();
            logEntries.Add(CreateSystemLog("TurnAdvanced", nextAfterDefeated is null ? "Time flows…" : $"Turn advanced to {nextAfterDefeated.DisplayName}.", combat));
            return new EnemyCombatTurnResolution(false, actor.Id.Value, null, [], logEntries, CombatRuntimeDto.FromDomain(combat));
        }

        if (!combat.HasLivingAllies)
        {
            combat.FailIfAllAlliesDefeated();
            logEntries.Add(CreateSystemLog("CombatFailed", "Combat failed.", combat));
            return new EnemyCombatTurnResolution(false, actor.Id.Value, null, [], logEntries, CombatRuntimeDto.FromDomain(combat));
        }

        var scriptedAction = TryResolveScriptedAction(combat, actor);

        CombatantSkill? skill;
        IReadOnlyCollection<Guid> targetIds;
        if (scriptedAction is not null)
        {
            skill = scriptedAction.Value.Skill;
            targetIds = scriptedAction.Value.TargetIds;
        }
        else
        {
            var plan = _planner.Plan(combat, actor);
            skill = plan?.Skill;
            targetIds = plan?.TargetIds ?? Array.Empty<Guid>();
        }

        if (skill is null)
        {
            logEntries.Add(CreateSystemLog("EnemyTurnResolved", $"{actor.DisplayName} has no usable skill.", combat, actor.Id.Value));
            SpendWastedTurn(combat, actor);
            logEntries.AddRange(AdvanceCombat(combat));
            return new EnemyCombatTurnResolution(true, actor.Id.Value, null, [], logEntries, CombatRuntimeDto.FromDomain(combat));
        }

        if (targetIds.Count == 0)
        {
            logEntries.Add(CreateSystemLog("EnemyTurnResolved", $"{actor.DisplayName} has no valid target.", combat, actor.Id.Value));
            SpendWastedTurn(combat, actor);
            logEntries.AddRange(AdvanceCombat(combat));
            return new EnemyCombatTurnResolution(true, actor.Id.Value, skill.Key, [], logEntries, CombatRuntimeDto.FromDomain(combat));
        }

        var validationResult = _actionValidator.Validate(combat, actor.Id.Value, skill.Key, targetIds);

        if (!validationResult.IsValid)
        {
            logEntries.Add(CreateSystemLog("EnemyTurnResolved", validationResult.ErrorMessage!, combat, actor.Id.Value));
            SpendWastedTurn(combat, actor);
            logEntries.AddRange(AdvanceCombat(combat));
            return new EnemyCombatTurnResolution(true, actor.Id.Value, skill.Key, targetIds, logEntries, CombatRuntimeDto.FromDomain(combat));
        }

        logEntries.Add(CreateSystemLog(
            "EnemyTurnResolved",
            $"{actor.DisplayName} uses {skill.DisplayName}.",
            combat,
            actor.Id.Value,
            skill.Key,
            targetIds));

        var effectResolution = _effectResolver.Resolve(combat, actor, skill, validationResult.Targets);
        logEntries.AddRange(effectResolution.LogEntries);

        // ATB: the enemy spends its gauge and enters recovery (heavier skills recover slower).
        combat.RegisterActionTaken(actor.Id.Value, AtbActionMath.RecoveryTicks(skill.BasePower, actor.BaseStatSnapshot.Recovery));

        logEntries.AddRange(AdvanceCombat(combat));

        return new EnemyCombatTurnResolution(
            true,
            actor.Id.Value,
            skill.Key,
            targetIds,
            logEntries,
            CombatRuntimeDto.FromDomain(combat));
    }
    // A turn that produced no executable action (no usable skill, no valid target, or a
    // failed validation) still consumes the actor's ATB gauge — otherwise the actor stays
    // "ready" forever, freezing its gauge full and looping the same failed turn.
    private static void SpendWastedTurn(Combat combat, Combatant actor)
    {
        combat.RegisterActionTaken(
            actor.Id.Value,
            AtbActionMath.RecoveryTicks(0, actor.BaseStatSnapshot.Recovery));
    }

    /// <summary>
    /// If a scripted <see cref="IBossBehavior"/> is registered for the active
    /// combatant's source key and it commits to an action this turn, resolve its
    /// chosen skill against the boss's own skill set. Returns <c>null</c> to defer
    /// to the generic enemy AI — when no behavior is registered, the behavior
    /// declines this turn, or the chosen skill key is not one the boss owns.
    /// </summary>
    private (CombatantSkill Skill, IReadOnlyCollection<Guid> TargetIds)? TryResolveScriptedAction(
        Combat combat,
        Combatant actor)
    {
        if (!_bossBehaviors.TryGetValue(actor.SourceKey, out var behavior))
        {
            return null;
        }

        var decision = behavior.DecideAction(new BossDecisionContext(combat, actor));

        if (decision is null)
        {
            return null;
        }

        var skill = actor.Skills.FirstOrDefault(
            s => string.Equals(s.Key, decision.SkillKey, StringComparison.OrdinalIgnoreCase));

        if (skill is null)
        {
            return null;
        }

        return (skill, decision.TargetIds);
    }

    private static IReadOnlyCollection<CombatLogEntryDto> AdvanceCombat(Combat combat)
    {
        combat.CompleteIfAllEnemiesDefeated();
        combat.FailIfAllAlliesDefeated();

        if (combat.Status == CombatStatus.Completed)
        {
            return [CreateSystemLog("CombatCompleted", "Combat completed.", combat)];
        }

        if (combat.Status == CombatStatus.Failed)
        {
            return [CreateSystemLog("CombatFailed", "Combat failed.", combat)];
        }

        // Real-time ATB: elect by readiness, never fast-forward the clock.
        combat.ElectActiveByReadiness();

        if (combat.Status == CombatStatus.Completed)
        {
            return [CreateSystemLog("CombatCompleted", "Combat completed.", combat)];
        }

        if (combat.Status == CombatStatus.Failed)
        {
            return [CreateSystemLog("CombatFailed", "Combat failed.", combat)];
        }

        var next = combat.GetActiveCombatant();
        return [CreateSystemLog("TurnAdvanced", next is null ? "Time flows…" : $"Turn advanced to {next.DisplayName}.", combat)];
    }

    private static EnemyCombatTurnResolution NotResolved(
        Combat combat,
        IReadOnlyCollection<CombatLogEntryDto> logEntries)
    {
        return new EnemyCombatTurnResolution(false, null, null, [], logEntries, CombatRuntimeDto.FromDomain(combat));
    }

    private static CombatLogEntryDto CreateSystemLog(
        string type,
        string message,
        Combat combat,
        Guid? actorId = null,
        string? skillKey = null,
        IReadOnlyCollection<Guid>? targetIds = null)
    {
        return new CombatLogEntryDto(
            OccurredAtUtc: DateTime.UtcNow,
            Type: type,
            Message: message,
            ActorId: actorId ?? combat.ActiveCombatantId?.Value,
            SkillKey: skillKey,
            TargetIds: targetIds ?? []);
    }
    public IReadOnlyCollection<CombatLogEntryDto> ResolveLeadingEnemyTurns(Combat combat)
    {
        var logs = new List<CombatLogEntryDto>();
        var safety = combat.Allies.Concat(combat.Enemies).Count() + 1;
        var iterations = 0;

        while (combat.Status == CombatStatus.Active)
        {
            var active = combat.GetActiveCombatant();
            if (active is null || active.Side != CombatantSide.Enemy)
                break;
            if (++iterations > safety)
                break;

            var resolution = Resolve(combat);
            logs.AddRange(resolution.LogEntries);
            if (!resolution.WasResolved)
                break;
        }

        return logs;
    }
}