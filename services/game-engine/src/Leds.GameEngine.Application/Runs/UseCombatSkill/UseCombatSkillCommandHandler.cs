using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.UseCombatSkill;

public sealed class UseCombatSkillCommandHandler
    : IRequestHandler<UseCombatSkillCommand, CombatSkillActionResult>
{
    private readonly IRunRepository _runRepository;
    private readonly ICombatSkillActionValidator _validator;
    private readonly ICombatSkillEffectResolver _effectResolver;
    private readonly IEnemyCombatTurnResolver _enemyTurnResolver;
    private readonly IRewardOfferRepository _rewardOfferRepository;
    private readonly RewardOfferFactory _rewardOfferFactory;
    private readonly IClock _clock;

    public UseCombatSkillCommandHandler(
        IRunRepository runRepository,
        ICombatSkillActionValidator validator,
        ICombatSkillEffectResolver effectResolver,
        IEnemyCombatTurnResolver enemyTurnResolver,
        IRewardOfferRepository rewardOfferRepository,
        RewardOfferFactory rewardOfferFactory,
        IClock clock)
    {
        _runRepository = runRepository;
        _validator = validator;
        _effectResolver = effectResolver;
        _enemyTurnResolver = enemyTurnResolver;
        _rewardOfferRepository = rewardOfferRepository;
        _rewardOfferFactory = rewardOfferFactory;
        _clock = clock;
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
            OccurredAtUtc: now.DateTime,
            Type: "ActionAccepted",
            Message: $"Skill '{request.SkillKey}' used by actor '{request.ActorId}'.",
            ActorId: request.ActorId,
            SkillKey: request.SkillKey,
            TargetIds: resolvedTargetIds);

        var effectResolution = _effectResolver.Resolve(
            run.ActiveCombat,
            validationResult.Actor!,
            validationResult.Skill!,
            validationResult.Targets);

        var progressionLogEntries = AdvanceCombat(effectResolution.Combat, now.DateTime);
        var enemyTurnLogEntries = ResolveEnemyTurns(effectResolution.Combat);
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

            var rewardOffer = CreateRewardOffer(combatNode);
            await _rewardOfferRepository.AddAsync(rewardOffer, cancellationToken);
            run.SetPendingRewardOffer(rewardOffer.Id);
        }
        else if (combatFailed)
        {
            run.FailActiveCombat(now);
        }

        await _runRepository.UpdateAsync(run, cancellationToken);

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

    private RewardOffer CreateRewardOffer(MapNode? combatNode)
    {
        var source = combatNode?.EventType switch
        {
            NodeEventType.Rare      => RewardSource.Rare,
            NodeEventType.Elite     => RewardSource.Elite,
            NodeEventType.RoomBoss  => RewardSource.RoomBoss,
            NodeEventType.FinalBoss => RewardSource.RoomBoss,
            _                       => RewardSource.Combat
        };

        return _rewardOfferFactory.CreateCombatRewardOffer(
            source,
            combatNode?.EventType ?? NodeEventType.Combat,
            combatNode?.RiskLevel ?? 25);
    }

    private IReadOnlyCollection<CombatLogEntryDto> ResolveEnemyTurns(Combat combat)
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

            var enemyResolution = _enemyTurnResolver.Resolve(combat);
            logEntries.AddRange(enemyResolution.LogEntries);
            resolvedTurnCount++;

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
