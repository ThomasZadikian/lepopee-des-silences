using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Combats.Metrics;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Domain.Combats.Atb;
using MediatR;

namespace Leds.GameEngine.Application.Runs.UseCombatSkill;

public sealed class UseCombatSkillCommandHandler
    : IRequestHandler<UseCombatSkillCommand, CombatSkillActionResult>
{
    private readonly IRunRepository _runRepository;
    private readonly ICombatSkillActionValidator _validator;
    private readonly ICombatSkillEffectResolver _effectResolver;
    private readonly IEnemyCombatTurnResolver _enemyTurnResolver;
    private readonly IClock _clock;
    private readonly ICombatActionRecordRepository _actionRecordRepository;
    private readonly ICombatResolutionService _combatResolution;
    private readonly IRewardOfferRepository _rewardOfferRepository;

    public UseCombatSkillCommandHandler(
        IRunRepository runRepository,
        ICombatSkillActionValidator validator,
        ICombatSkillEffectResolver effectResolver,
        IEnemyCombatTurnResolver enemyTurnResolver,
        IClock clock,
        ICombatActionRecordRepository actionRecordRepository,
        ICombatResolutionService combatResolution,
        IRewardOfferRepository rewardOfferRepository)
    {
        _runRepository = runRepository;
        _validator = validator;
        _effectResolver = effectResolver;
        _enemyTurnResolver = enemyTurnResolver;
        _clock = clock;
        _actionRecordRepository = actionRecordRepository;
        _combatResolution = combatResolution;
        _rewardOfferRepository = rewardOfferRepository;
    }

    public async Task<CombatSkillActionResult> Handle(
        UseCombatSkillCommand request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.Status != RunStatus.Active)
        {
            throw new ConflictException("Run must be active to submit a combat action.");
        }

        if (!run.HasActiveCombat)
        {
            throw new ConflictException("Run has no active combat.");
        }

        var combatId = new CombatId(request.CombatId);

        if (run.ActiveCombat is null)
        {
            throw new ConflictException("Run has no active combat.");
        }

        if (run.ActiveCombat.Id != combatId)
        {
            throw new ConflictException("Combat does not match the active run combat.");
        }

        run.ActiveCombat.EnsureActorCanAct(request.ActorId);

        var validationResult = _validator.Validate(
            run.ActiveCombat,
            request.ActorId,
            request.SkillKey,
            request.TargetIds);

        if (!validationResult.IsValid)
        {
            throw new ConflictException(validationResult.ErrorMessage!);
        }

        var now = _clock.UtcNow;
        var resolvedTargetIds = validationResult.Targets
            .Select(t => t.Id.Value)
            .ToArray();

        var logEntry = new CombatLogEntryDto(
            OccurredAtUtc: now.UtcDateTime,
            Type: "ActionAccepted",
            Message: $"Skill '{request.SkillKey}' used by actor '{request.ActorId}'.",
            ActorId: request.ActorId,
            SkillKey: request.SkillKey,
            TargetIds: resolvedTargetIds);

        var beforeSnapshots = CombatMetricsCalculator.SnapshotTargets(validationResult.Targets);

        var effectResolution = _effectResolver.Resolve(
            run.ActiveCombat,
            validationResult.Actor!,
            validationResult.Skill!,
            validationResult.Targets);

        // ATB: the actor spends its gauge and enters recovery (heavier skills recover slower).
        effectResolution.Combat.RegisterActionTaken(
            validationResult.Actor!.Id.Value,
            AtbActionMath.RecoveryTicks(validationResult.Skill!.BasePower, validationResult.Actor!.BaseStatSnapshot.Recovery));

        var progressionLogEntries = AdvanceCombat(effectResolution.Combat, now.UtcDateTime);

        var allActionRecords = new List<CombatActionRecord>();
        var playerActionRecords = CombatMetricsCalculator.CalculateActionRecords(
            run.ActiveCombat.Id.Value,
            run.ActiveCombat.TurnNumber,
            validationResult.Actor!,
            validationResult.Skill!,
            validationResult.Targets,
            beforeSnapshots,
            now.UtcDateTime);
        allActionRecords.AddRange(playerActionRecords);

        // ATB: the player's action resolves the player's turn only. Enemy turns are
        // driven by the client in real time via AdvanceCombatTurnCommand as gauges fill.
        var enemyTurnLogEntries = Array.Empty<CombatLogEntryDto>();
        var finalCombat = effectResolution.Combat;
        var combatCompleted = finalCombat.Status == CombatStatus.Completed;
        var combatFailed = finalCombat.Status == CombatStatus.Failed;

        SyncPlayerStateFromCombat(run, finalCombat);

        var rewardOffer = _combatResolution.ApplyOutcome(run, finalCombat, now);

        await _runRepository.UpdateAsync(run, cancellationToken);
        if (rewardOffer is not null)
        {
            await _rewardOfferRepository.AddAsync(run.Id, rewardOffer, cancellationToken);
        }

        await _actionRecordRepository.AddRangeAsync(allActionRecords, cancellationToken);

        var logEntries = new[] { logEntry }
            .Concat(effectResolution.LogEntries)
            .Concat(progressionLogEntries)
            .Concat(enemyTurnLogEntries)
            .ToArray();

        return new CombatSkillActionResult(
            CombatId: finalCombat.Id.Value,
            ActorId: request.ActorId,
            SkillKey: request.SkillKey,
            TargetIds: resolvedTargetIds,
            Accepted: true,
            Message: null,
            Combat: CombatRuntimeDto.FromDomain(finalCombat, CombatItemHelper.GetUsableBattleItems(run)),
            LogEntries: logEntries,
            CombatCompleted: combatCompleted,
            CombatFailed: combatFailed,
            CanProgressRun: combatCompleted,
            RunStatus: run.Status.ToString());
    }

    private IReadOnlyCollection<CombatLogEntryDto> ResolveEnemyTurns(
        Combat combat,
        List<CombatActionRecord> actionRecords,
        Guid combatId,
        DateTimeOffset now)
    {
        var logEntries = new List<CombatLogEntryDto>();
        var maxAutoTurns = combat.Allies.Concat(combat.Enemies).Count(c => !c.IsDefeated) + 1;
        var resolvedTurnCount = 0;

        while (combat.Status == CombatStatus.Active)
        {
            var activeCombatant = combat.GetActiveCombatant();

            if (activeCombatant.Side != CombatantSide.Enemy)
            {
                break;
            }

            if (resolvedTurnCount >= maxAutoTurns)
            {
                throw new ConflictException("Automatic enemy turn resolution exceeded the safety limit.");
            }

            var allLiving = combat.Allies.Concat(combat.Enemies).Where(c => !c.IsDefeated).ToArray();
            var beforeSnapshots = CombatMetricsCalculator.SnapshotTargets(allLiving);

            var enemyResolution = _enemyTurnResolver.Resolve(combat);
            logEntries.AddRange(enemyResolution.LogEntries);
            resolvedTurnCount++;

            if (enemyResolution.WasResolved && enemyResolution.ActorId.HasValue)
            {
                var enemyActor = allLiving.FirstOrDefault(c => c.Id.Value == enemyResolution.ActorId.Value);
                var enemySkillKey = enemyResolution.SkillKey;

                if (enemyActor is not null && enemySkillKey is not null)
                {
                    var affectedTargets = allLiving
                        .Where(c => c.Id.Value != enemyActor.Id.Value)
                        .ToArray();

                    var enemySkill = enemyActor.Skills.FirstOrDefault(s =>
                        string.Equals(s.Key, enemySkillKey, StringComparison.OrdinalIgnoreCase));

                    if (enemySkill is not null && affectedTargets.Length > 0)
                    {
                        var enemyRecords = CombatMetricsCalculator.CalculateActionRecords(
                            combatId,
                            combat.TurnNumber,
                            enemyActor,
                            enemySkill,
                            affectedTargets,
                            beforeSnapshots,
                            now.UtcDateTime);
                        actionRecords.AddRange(enemyRecords);
                    }
                }
            }

            if (!enemyResolution.WasResolved)
            {
                break;
            }
        }

        return logEntries;
    }

    private static IReadOnlyCollection<CombatLogEntryDto> AdvanceCombat(
        Combat combat,
        DateTime occurredAtUtc)
    {
        combat.CompleteIfAllEnemiesDefeated();
        combat.FailIfAllAlliesDefeated();

        if (combat.Status == CombatStatus.Completed)
        {
            return
            [
                CreateSystemLog(occurredAtUtc, "CombatCompleted", "Combat completed.", combat)
            ];
        }

        if (combat.Status == CombatStatus.Failed)
        {
            return
            [
                CreateSystemLog(occurredAtUtc, "CombatFailed", "Combat failed.", combat)
            ];
        }

        combat.AdvanceTurn();

        if (combat.Status == CombatStatus.Completed)
        {
            return
            [
                CreateSystemLog(occurredAtUtc, "CombatCompleted", "Combat completed.", combat)
            ];
        }

        if (combat.Status == CombatStatus.Failed)
        {
            return
            [
                CreateSystemLog(occurredAtUtc, "CombatFailed", "Combat failed.", combat)
            ];
        }

        var activeCombatant = combat.GetActiveCombatant();

        return
        [
            CreateSystemLog(
                occurredAtUtc,
                "TurnAdvanced",
                $"Turn advanced to {activeCombatant.DisplayName}.",
                combat)
        ];
    }

    private static CombatLogEntryDto CreateSystemLog(
        DateTime occurredAtUtc,
        string type,
        string message,
        Combat combat)
    {
        return new CombatLogEntryDto(
            OccurredAtUtc: occurredAtUtc,
            Type: type,
            Message: message,
            ActorId: combat.ActiveCombatantId?.Value,
            SkillKey: null,
            TargetIds: []);
    }

    private static void SyncPlayerStateFromCombat(Run run, Combat combat)
    {
        var playerCombatant = combat.Allies.FirstOrDefault(a => a.Side == CombatantSide.Player);
        if (playerCombatant is null) return;

        run.PlayerState.SyncFromCombat(
            playerCombatant.CurrentVitality,
            playerCombatant.Guard,
            playerCombatant.Mana,
            playerCombatant.Charge);
    }
}
