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
    RewardOffer? ApplyOutcome(Run run, Combat combat, DateTimeOffset now);
}

public sealed class CombatResolutionService : ICombatResolutionService
{
    private readonly RewardOfferFactory _rewardOfferFactory;

    public CombatResolutionService(RewardOfferFactory rewardOfferFactory)
    {
        _rewardOfferFactory = rewardOfferFactory;
    }

    public RewardOffer? ApplyOutcome(
        Run run,
        Combat combat,
        DateTimeOffset now)
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
                run.SetPendingRewardOffer(rewardOffer.Id);
                return rewardOffer;

            case CombatStatus.Failed:
                run.FailActiveCombat(now);
                return null;

            default:
                return null;
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
