using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.Application.Rewards.RewardOfferFactory;

public sealed class RewardOfferFactory
{
    private readonly ICombatRiskProfileResolver _riskProfileResolver;

    public RewardOfferFactory(ICombatRiskProfileResolver riskProfileResolver)
    {
        _riskProfileResolver = riskProfileResolver;
    }

    /// <summary>
    /// Creates a combat reward offer for the given source tier and node risk level.
    /// The <paramref name="eventType"/> is forwarded to <see cref="ICombatRiskProfileResolver"/>
    /// so that the resulting <see cref="RewardOffer.CombatScaling"/> reflects the exact
    /// tier base-risk and computed multipliers.
    /// </summary>
    public RewardOffer CreateCombatRewardOffer(
        RewardSource source,
        NodeEventType eventType,
        int riskLevel)
    {
        var scaling = _riskProfileResolver.Resolve(eventType, riskLevel);

        var choices = source switch
        {
            RewardSource.RoomBoss => CreateBossRewardChoices(riskLevel),
            RewardSource.Elite    => CreateEliteRewardChoices(riskLevel),
            RewardSource.Rare     => CreateRareRewardChoices(riskLevel),
            _                     => CreateCombatRewardChoices(riskLevel)
        };

        return RewardOffer.Create(source, choices, scaling);
    }

    private static List<RewardChoice> CreateCombatRewardChoices(int riskLevel)
    {
        var healAmount = riskLevel / 5 + 10;

        return new List<RewardChoice>
        {
            RewardChoice.Create(
                RewardType.Heal,
                "Soin léger",
                $"Récupère {healAmount} PV.",
                $"heal:{healAmount}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Souffle retrouvé",
                $"Récupère {healAmount + 4} PV.",
                $"heal:{healAmount + 4}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Calme intérieur",
                $"Récupère {healAmount + 8} PV.",
                $"heal:{healAmount + 8}")
        };
    }

    private static List<RewardChoice> CreateRareRewardChoices(int riskLevel)
    {
        var healAmount = riskLevel / 4 + 15;

        return new List<RewardChoice>
        {
            RewardChoice.Create(
                RewardType.Heal,
                "Soin rare",
                $"Récupère {healAmount} PV.",
                $"heal:{healAmount}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Répit lucide",
                $"Récupère {healAmount + 5} PV.",
                $"heal:{healAmount + 5}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Soin substantiel",
                $"Récupère {healAmount + 10} PV.",
                $"heal:{healAmount + 10}")
        };
    }

    private static List<RewardChoice> CreateEliteRewardChoices(int riskLevel)
    {
        var healAmount = riskLevel / 3 + 20;

        return new List<RewardChoice>
        {
            RewardChoice.Create(
                RewardType.Heal,
                "Soin important",
                $"Récupère {healAmount} PV.",
                $"heal:{healAmount}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Volonté restaurée",
                $"Récupère {healAmount + 8} PV.",
                $"heal:{healAmount + 8}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Suture mentale",
                $"Récupère {healAmount + 16} PV.",
                $"heal:{healAmount + 16}")
        };
    }

    private static List<RewardChoice> CreateBossRewardChoices(int riskLevel)
    {
        var healAmount = riskLevel / 2 + 30;

        return new List<RewardChoice>
        {
            RewardChoice.Create(
                RewardType.Heal,
                "Soin majeur",
                $"Récupère {healAmount} PV.",
                $"heal:{healAmount}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Souffle du Gardien",
                $"Récupère {healAmount + 12} PV.",
                $"heal:{healAmount + 12}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Silence recomposé",
                $"Récupère {healAmount + 24} PV.",
                $"heal:{healAmount + 24}")
        };
    }
}
