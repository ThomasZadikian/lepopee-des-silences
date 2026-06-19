using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Combats.Resolution;

public interface ICombatResolutionService
{
    /// <summary>
    /// Applique LA conséquence métier d'une fin de combat (victoire ou défaite),
    /// quel que soit le point d'entrée. Source unique de vérité.
    /// </summary>
    Task ApplyOutcomeAsync(Run run, Combat combat, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed class CombatResolutionService : ICombatResolutionService
{
    private readonly IRewardOfferRepository _rewardOfferRepository;
    private readonly RewardOfferFactory _rewardOfferFactory;

    public CombatResolutionService(
        IRewardOfferRepository rewardOfferRepository,
        RewardOfferFactory rewardOfferFactory)
    {
        _rewardOfferRepository = rewardOfferRepository;
        _rewardOfferFactory = rewardOfferFactory;
    }

    public async Task ApplyOutcomeAsync(
        Run run,
        Combat combat,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        switch (combat.Status)
        {
            case CombatStatus.Completed:
                // Doit être lu AVANT de compléter (l'état du nœud peut changer).
                var combatNode = run.CurrentRoom.Nodes.SingleOrDefault(n =>
                    n.State == NodeState.Selected &&
                    n.Row == run.CurrentRoom.CurrentNodeDepth);

                run.CompleteActiveCombat();
                run.ConsumeNextCombatModifiers();

                var rewardOffer = CreateRewardOffer(combatNode);
                await _rewardOfferRepository.AddAsync(rewardOffer, cancellationToken);
                run.SetPendingRewardOffer(rewardOffer.Id);
                break;

            case CombatStatus.Failed:
                run.FailActiveCombat(now);
                break;
        }
    }

    private RewardOffer CreateRewardOffer(MapNode? combatNode)
    {
        var source = combatNode?.EventType switch
        {
            NodeEventType.Rare => RewardSource.Rare,
            NodeEventType.Elite => RewardSource.Elite,
            NodeEventType.RoomBoss => RewardSource.RoomBoss,
            NodeEventType.FinalBoss => RewardSource.RoomBoss,
            _ => RewardSource.Combat
        };

        return _rewardOfferFactory.CreateCombatRewardOffer(
            source,
            combatNode?.EventType ?? NodeEventType.Combat,
            combatNode?.RiskLevel ?? 25);
    }
}