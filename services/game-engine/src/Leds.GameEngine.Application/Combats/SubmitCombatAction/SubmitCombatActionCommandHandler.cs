using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Combats.Metrics;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using MediatR;

namespace Leds.GameEngine.Application.Combats.SubmitCombatAction;

public sealed class SubmitCombatActionCommandHandler
    : IRequestHandler<SubmitCombatActionCommand, SubmitCombatActionResponse>
{
    private const string BasicAttackSkillKey = "skill.basic.strike";

    private readonly IRunRepository _runRepository;
    private readonly ICombatSkillActionValidator _validator;
    private readonly ICombatSkillEffectResolver _effectResolver;
    private readonly IEnemyCombatTurnResolver _enemyTurnResolver;
    private readonly IRewardOfferRepository _rewardOfferRepository;
    private readonly RewardOfferFactory _rewardOfferFactory;
    private readonly ICombatActionRecordRepository _actionRecordRepository;
    private readonly IClock _clock;

    public SubmitCombatActionCommandHandler(
        IRunRepository runRepository,
        ICombatSkillActionValidator validator,
        ICombatSkillEffectResolver effectResolver,
        IEnemyCombatTurnResolver enemyTurnResolver,
        IRewardOfferRepository rewardOfferRepository,
        RewardOfferFactory rewardOfferFactory,
        ICombatActionRecordRepository actionRecordRepository,
        IClock clock)
    {
        _runRepository = runRepository;
        _validator = validator;
        _effectResolver = effectResolver;
        _enemyTurnResolver = enemyTurnResolver;
        _rewardOfferRepository = rewardOfferRepository;
        _rewardOfferFactory = rewardOfferFactory;
        _actionRecordRepository = actionRecordRepository;
        _clock = clock;
    }

    public async Task<SubmitCombatActionResponse> Handle(
        SubmitCombatActionCommand request,
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
            throw new DomainException("Run must be active to submit a combat action.");
        }

        if (!run.HasActiveCombat)
        {
            throw new DomainException("Run has no active combat.");
        }

        var combatId = new CombatId(request.CombatId);

        if (run.ActiveCombatId != combatId)
        {
            throw new DomainException("Combat does not belong to the active run.");
        }

        var combat = run.ActiveCombat!;

        if (request.ActionType != "BasicAttack")
        {
            throw new DomainException($"Combat action type '{request.ActionType}' is not supported.");
        }

        var actorGuid = request.ActorId;
        combat.EnsureActorCanAct(actorGuid);

        var targetIds = new[] { request.TargetId };
        var skillKey = BasicAttackSkillKey;

        var validationResult = _validator.Validate(combat, actorGuid, skillKey, targetIds);
        if (!validationResult.IsValid)
        {
            throw new DomainException(validationResult.ErrorMessage!);
        }

        var now = _clock.UtcNow;

        var beforeSnapshots = CombatMetricsCalculator.SnapshotTargets(validationResult.Targets);

        var effectResolution = _effectResolver.Resolve(
            combat, validationResult.Actor!, validationResult.Skill!, validationResult.Targets);

        AdvanceCombat(effectResolution.Combat);

        var playerActionRecords = CombatMetricsCalculator.CalculateActionRecords(
            combat.Id.Value, combat.TurnNumber,
            validationResult.Actor!, validationResult.Skill!,
            validationResult.Targets, beforeSnapshots, now.DateTime);

        var allActionRecords = new List<CombatActionRecord>();
        allActionRecords.AddRange(playerActionRecords);

        AdvancePastEnemyTurns(effectResolution.Combat);

        var finalCombat = effectResolution.Combat;
        var combatCompleted = finalCombat.Status == CombatStatus.Completed;
        var combatFailed = finalCombat.Status == CombatStatus.Failed;

        SyncPlayerStateFromCombat(run, finalCombat);

        if (combatCompleted)
        {
            var combatNode = run.CurrentRoom.Nodes.SingleOrDefault(n =>
                n.State == NodeState.Selected &&
                n.Row == run.CurrentRoom.CurrentNodeDepth);

            run.CompleteActiveCombat();
            run.ConsumeNextCombatModifiers();

            var source = combatNode?.EventType switch
            {
                NodeEventType.Rare => RewardSource.Rare,
                NodeEventType.Elite => RewardSource.Elite,
                NodeEventType.RoomBoss => RewardSource.RoomBoss,
                NodeEventType.FinalBoss => RewardSource.RoomBoss,
                _ => RewardSource.Combat
            };

            var rewardOffer = _rewardOfferFactory.CreateCombatRewardOffer(
                source,
                combatNode?.EventType ?? NodeEventType.Combat,
                combatNode?.RiskLevel ?? 25);
            await _rewardOfferRepository.AddAsync(rewardOffer, cancellationToken);
            run.SetPendingRewardOffer(rewardOffer.Id);
        }
        else if (combatFailed)
        {
            run.FailActiveCombat(now);
        }

        await _actionRecordRepository.AddRangeAsync(allActionRecords, cancellationToken);
        await _runRepository.UpdateAsync(run, cancellationToken);

        var target = finalCombat.Enemies
            .FirstOrDefault(e => e.Id.Value == request.TargetId);

        var resultDto = new CombatActionResultDto(
            CombatId: finalCombat.Id.Value,
            ActorId: request.ActorId,
            TargetId: request.TargetId,
            ActionType: "BasicAttack",
            Damage: target is { IsDefeated: true } ? target.MaxVitality : 0,
            TargetRemainingHealth: target?.CurrentVitality ?? 0,
            TargetDefeated: target?.IsDefeated ?? false,
            CombatState: finalCombat.Status.ToString(),
            WinningSide: finalCombat.Status == CombatStatus.Completed
                ? CombatantSide.Player.ToString()
                : null,
            NextActorId: finalCombat.ActiveCombatantId?.Value,
            Round: finalCombat.TurnNumber);

        return new SubmitCombatActionResponse(
            RunDto.FromDomain(run),
            resultDto);
    }

    private IReadOnlyCollection<CombatActionRecord> ResolveEnemyTurns(
        Combat combat,
        List<CombatActionRecord> actionRecords,
        Guid combatId,
        DateTime now)
    {
        var maxAutoTurns = combat.Allies.Concat(combat.Enemies).Count(c => !c.IsDefeated) + 1;
        var resolvedTurnCount = 0;

        while (combat.Status == CombatStatus.Active)
        {
            var activeCombatant = combat.GetActiveCombatant();

            if (activeCombatant.Side != CombatantSide.Enemy) break;
            if (resolvedTurnCount >= maxAutoTurns) break;

            var allLiving = combat.Allies.Concat(combat.Enemies).Where(c => !c.IsDefeated).ToArray();
            var beforeSnapshots = CombatMetricsCalculator.SnapshotTargets(allLiving);

            var enemyResolution = _enemyTurnResolver.Resolve(combat);
            resolvedTurnCount++;

            if (enemyResolution.WasResolved && enemyResolution.ActorId.HasValue)
            {
                var enemyActor = allLiving.FirstOrDefault(c => c.Id.Value == enemyResolution.ActorId.Value);
                var skillKey = enemyResolution.SkillKey;

                if (enemyActor is not null && skillKey is not null)
                {
                    var affectedTargets = allLiving
                        .Where(c => c.Id.Value != enemyActor.Id.Value)
                        .ToArray();

                    var enemySkill = enemyActor.Skills.FirstOrDefault(s =>
                        string.Equals(s.Key, skillKey, StringComparison.OrdinalIgnoreCase));

                    if (enemySkill is not null && affectedTargets.Length > 0)
                    {
                        var enemyRecords = CombatMetricsCalculator.CalculateActionRecords(
                            combatId, combat.TurnNumber,
                            enemyActor, enemySkill, affectedTargets,
                            beforeSnapshots, now);
                        actionRecords.AddRange(enemyRecords);
                    }
                }
            }

            if (!enemyResolution.WasResolved) break;
        }

        return actionRecords;
    }

    private static void AdvanceCombat(Combat combat)
    {
        combat.CompleteIfAllEnemiesDefeated();
        combat.FailIfAllAlliesDefeated();

        if (combat.Status != CombatStatus.Active) return;

        combat.AdvanceTurn();
    }

    private static void AdvancePastEnemyTurns(Combat combat)
    {
        var maxAutoTurns = combat.Allies.Concat(combat.Enemies).Count(c => !c.IsDefeated) + 1;
        var advancedTurns = 0;

        while (combat.Status == CombatStatus.Active && advancedTurns < maxAutoTurns)
        {
            var activeCombatant = combat.GetActiveCombatant();

            if (activeCombatant.Side != CombatantSide.Enemy)
            {
                return;
            }

            combat.AdvanceTurn();
            advancedTurns++;
        }
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
