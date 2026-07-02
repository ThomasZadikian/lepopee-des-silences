using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Combats.EnemyTurns.Ai;
using Leds.GameEngine.Application.Combats.Metrics;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using MediatR;

namespace Leds.GameEngine.Application.Runs.HoldCombatTurn;

public sealed class HoldCombatTurnCommandHandler
    : IRequestHandler<HoldCombatTurnCommand, CombatSkillActionResult>
{
    private const int MaxEnemyTurnsPerHold = 8;

    private readonly IRunRepository _runRepository;
    private readonly IEnemyCombatTurnResolver _enemyTurnResolver;
    private readonly IClock _clock;
    private readonly ICombatActionRecordRepository _actionRecordRepository;
    private readonly ICombatResolutionService _combatResolution;
    private readonly IRewardOfferRepository _rewardOfferRepository;

    public HoldCombatTurnCommandHandler(
        IRunRepository runRepository,
        IEnemyCombatTurnResolver enemyTurnResolver,
        IClock clock,
        ICombatActionRecordRepository actionRecordRepository,
        ICombatResolutionService combatResolution,
        IRewardOfferRepository rewardOfferRepository)
    {
        _runRepository = runRepository;
        _enemyTurnResolver = enemyTurnResolver;
        _clock = clock;
        _actionRecordRepository = actionRecordRepository;
        _combatResolution = combatResolution;
        _rewardOfferRepository = rewardOfferRepository;
    }

    public async Task<CombatSkillActionResult> Handle(
        HoldCombatTurnCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.Status != RunStatus.Active)
        {
            throw new ConflictException("Run must be active to advance combat.");
        }

        if (!run.HasActiveCombat || run.ActiveCombat is null)
        {
            throw new ConflictException("Run has no active combat.");
        }

        if (run.ActiveCombat.Id != new CombatId(request.CombatId))
        {
            throw new ConflictException("Combat does not match the active run combat.");
        }

        var combat = run.ActiveCombat;
        var now = _clock.UtcNow;
        var logEntries = new List<CombatLogEntryDto>();
        var actionRecords = new List<CombatActionRecord>();

        if (combat.Status == CombatStatus.Active)
        {
            // The ally that holds the selection before enemies act — restored after
            // the enemy loop so an enemy turn within this tick can't steal it.
            var heldActive = combat.GetActiveCombatant();
            Guid? heldAllyId = heldActive is { Side: CombatantSide.Player }
                ? heldActive.Id.Value
                : null;

            var readyEnemyIds = combat.HoldTick(request.DeltaTicks);

            // Durable effects (poison/regen/buff expiry) resolve as time passed.
            foreach (var statusTick in combat.TickAllStatusEffects())
            {
                logEntries.Add(BuildStatusLogEntry(statusTick, now.UtcDateTime));
            }

            var resolved = 0;

            foreach (var enemyId in readyEnemyIds)
            {
                if (combat.Status != CombatStatus.Active || resolved >= MaxEnemyTurnsPerHold)
                {
                    break;
                }

                var candidate = combat.Enemies.FirstOrDefault(e => e.Id.Value == enemyId);
                if (candidate is not null && EnemyBoostHeuristic.ShouldHoldForMoreCharge(candidate, combat.Id.Value))
                {
                    continue; // still banking overflow — its gauge keeps climbing via HoldTick, revisited next hold call
                }

                combat.MakeActiveCombatant(enemyId);

                var allLiving = combat.Allies.Concat(combat.Enemies).Where(c => !c.IsDefeated).ToArray();
                var beforeSnapshots = CombatMetricsCalculator.SnapshotTargets(allLiving);

                var resolution = _enemyTurnResolver.Resolve(combat);
                logEntries.AddRange(resolution.LogEntries);
                resolved++;

                if (resolution.WasResolved && resolution.ActorId.HasValue && resolution.SkillKey is not null)
                {
                    var enemyActor = allLiving.FirstOrDefault(c => c.Id.Value == resolution.ActorId.Value);
                    var enemySkill = enemyActor?.Skills.FirstOrDefault(s =>
                        string.Equals(s.Key, resolution.SkillKey, StringComparison.OrdinalIgnoreCase));

                    if (enemyActor is not null && enemySkill is not null)
                    {
                        var affected = allLiving.Where(c => c.Id.Value != enemyActor.Id.Value).ToArray();
                        if (affected.Length > 0)
                        {
                            actionRecords.AddRange(CombatMetricsCalculator.CalculateActionRecords(
                                combat.Id.Value,
                                combat.TurnNumber,
                                enemyActor,
                                enemySkill,
                                affected,
                                beforeSnapshots,
                                now.UtcDateTime));
                        }
                    }
                }
            }

            // After any ready enemies have acted, restore the ally that held the
            // selection (if still ready), else hand the slot to the next ready ally.
            combat.ElectActiveByReadiness(heldAllyId);
        }

        var finalCombat = combat;
        var combatCompleted = finalCombat.Status == CombatStatus.Completed;
        var combatFailed = finalCombat.Status == CombatStatus.Failed;

        SyncPlayerStateFromCombat(run, finalCombat);

        var rewardOffer = await _combatResolution.ApplyOutcomeAsync(run, finalCombat, now, cancellationToken);

        // Hot path on a normal tick (combat still active, no run-level change);
        // fall back to the full run write only when the combat ends or a reward
        // offer was produced.
        if (combatCompleted || combatFailed || rewardOffer is not null)
        {
            await _runRepository.UpdateAsync(run, cancellationToken);
        }
        else
        {
            await _runRepository.UpdateActiveCombatStateAsync(run, cancellationToken);
        }

        if (rewardOffer is not null)
        {
            await _rewardOfferRepository.AddAsync(run.Id, rewardOffer, cancellationToken);
        }

        return new CombatSkillActionResult(
            CombatId: finalCombat.Id.Value,
            ActorId: finalCombat.ActiveCombatantId?.Value ?? Guid.Empty,
            SkillKey: null,
            TargetIds: [],
            Accepted: true,
            Message: null,
            Combat: CombatRuntimeDto.FromDomain(finalCombat, CombatItemHelper.GetUsableBattleItems(run)),
            LogEntries: logEntries,
            CombatCompleted: combatCompleted,
            CombatFailed: combatFailed,
            CanProgressRun: combatCompleted,
            RunStatus: run.Status.ToString());
    }

    private static CombatLogEntryDto BuildStatusLogEntry(CombatantStatusTick tick, DateTime occurredAtUtc)
    {
        var (type, message) = tick.Event switch
        {
            { Expired: true } e => ("StatusExpired", $"{tick.CombatantName}: {e.DisplayName} fades."),
            { Kind: StatusEffectKind.DamageOverTime } e => ("DamageApplied", $"{tick.CombatantName} suffers {e.Amount} from {e.DisplayName}."),
            { Kind: StatusEffectKind.HealOverTime } e => ("HealApplied", $"{tick.CombatantName} regenerates {e.Amount} from {e.DisplayName}."),
            var e => ("StatusTick", $"{tick.CombatantName}: {e.DisplayName}.")
        };

        return new CombatLogEntryDto(
            OccurredAtUtc: occurredAtUtc,
            Type: type,
            Message: message,
            ActorId: tick.CombatantId,
            SkillKey: null,
            TargetIds: new[] { tick.CombatantId });
    }

    private static void SyncPlayerStateFromCombat(Run run, Combat combat)
    {
        var playerCombatant = combat.Allies.FirstOrDefault(a => a.Side == CombatantSide.Player);
        if (playerCombatant is null)
        {
            return;
        }

        run.PlayerState.SyncFromCombat(
            playerCombatant.CurrentVitality,
            playerCombatant.Guard,
            playerCombatant.Mana,
            playerCombatant.Charge);
    }
}