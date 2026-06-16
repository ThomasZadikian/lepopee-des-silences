using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Combats.SubmitCombatAction;

public sealed class SubmitCombatActionCommandHandler
    : IRequestHandler<SubmitCombatActionCommand, SubmitCombatActionResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly ICombatInstanceRepository _combatInstanceRepository;
    private readonly IRewardOfferRepository _rewardOfferRepository;
    private readonly RewardOfferFactory _rewardOfferFactory;
    private readonly IClock _clock;

    public SubmitCombatActionCommandHandler(
        IRunRepository runRepository,
        ICombatInstanceRepository combatInstanceRepository,
        IRewardOfferRepository rewardOfferRepository,
        RewardOfferFactory rewardOfferFactory,
        IClock clock)
    {
        _runRepository = runRepository;
        _combatInstanceRepository = combatInstanceRepository;
        _rewardOfferRepository = rewardOfferRepository;
        _rewardOfferFactory = rewardOfferFactory;
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

        var combat = await _combatInstanceRepository.GetByIdAsync(combatId, cancellationToken);

        if (combat is null)
        {
            throw new NotFoundException("Combat", request.CombatId);
        }

        if (request.ActionType != "BasicAttack")
        {
            throw new DomainException($"Combat action type '{request.ActionType}' is not supported.");
        }

        var actorId = new CombatantId(request.ActorId);
        var targetId = new CombatantId(request.TargetId);

        var action = CombatAction.BasicAttack(actorId, targetId);

        var result = combat.SubmitAction(action);

        while (result.CombatState == CombatState.InProgress)
        {
            var currentActor = combat.Combatants.Single(c => c.Id == result.NextActorId);

            if (currentActor.Side != CombatantSide.Enemy)
            {
                break;
            }

            var target = combat.Combatants.First(c => c.Side == CombatantSide.Player && !c.IsDefeated);

            var enemyAction = CombatAction.BasicAttack(currentActor.Id, target.Id);

            result = combat.SubmitAction(enemyAction);

            if (result.CombatState == CombatState.Completed)
            {
                break;
            }
        }

        if (result.CombatState == CombatState.Completed)
        {
            if (result.WinningSide == CombatantSide.Player)
            {
                // Capturer le nœud AVANT CompleteActiveCombat : celui-ci appelle
                // ResolveCurrentEvent() qui fait passer le nœud Selected → Resolved.
                var room = run.CurrentRoom;
                var combatNode = room.Nodes.SingleOrDefault(n =>
                    n.State == NodeState.Selected &&
                    n.Row == room.CurrentNodeDepth);

                run.CompleteActiveCombat(combat.Id);

                var source = combatNode?.EventType switch
                {
                    NodeEventType.Rare => RewardSource.Rare,
                    NodeEventType.Elite => RewardSource.Elite,
                    NodeEventType.RoomBoss => RewardSource.RoomBoss,
                    NodeEventType.FinalBoss => RewardSource.RoomBoss,
                    _ => RewardSource.Combat
                };

                var riskLevel = combatNode?.RiskLevel ?? 25;
                var nodeEventType = combatNode?.EventType ?? NodeEventType.Combat;
                var rewardOffer = _rewardOfferFactory.CreateCombatRewardOffer(source, nodeEventType, riskLevel);

                await _rewardOfferRepository.AddAsync(rewardOffer, cancellationToken);

                run.SetPendingRewardOffer(rewardOffer.Id);
            }
            else
            {
                run.FailActiveCombat(combat.Id, _clock.UtcNow);
            }
        }

        await _combatInstanceRepository.UpdateAsync(combat, cancellationToken);

        await _runRepository.UpdateAsync(run, cancellationToken);

        return new SubmitCombatActionResponse(
            RunDto.FromDomain(run),
            CombatAction