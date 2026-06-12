using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.Application.Rewards.RewardOfferFactory;

public sealed class RewardOfferFactory
{
    private readonly ICombatRiskProfileResolver _riskProfileResolver;
    private readonly RewardPowerScaler _rewardPowerScaler = new();

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
            RewardSource.RoomBoss => CreateBossRewardChoices(riskLevel, scaling.RewardPowerMultiplier),
            RewardSource.Elite    => CreateEliteRewardChoices(riskLevel, scaling.RewardPowerMultiplier),
            RewardSource.Rare     => CreateRareRewardChoices(riskLevel, scaling.RewardPowerMultiplier),
            _                     => CreateCombatRewardChoices(riskLevel, scaling.RewardPowerMultiplier)
        };

        return RewardOffer.Create(source, choices, scaling);
    }

    private List<RewardChoice> CreateCombatRewardChoices(int riskLevel, double multiplier)
    {
        var baseHeal = riskLevel / 5 + 10;
        var healAmount = _rewardPowerScaler.ScaleAmount(baseHeal, multiplier);

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
                $"Récupère {_rewardPowerScaler.ScaleAmount(baseHeal + 4, multiplier)} PV.",
                $"heal:{_rewardPowerScaler.ScaleAmount(baseHeal + 4, multiplier)}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Calme intérieur",
                $"Récupère {_rewardPowerScaler.ScaleAmount(baseHeal + 8, multiplier)} PV.",
                $"heal:{_rewardPowerScaler.ScaleAmount(baseHeal + 8, multiplier)}")
        };
    }

    private List<RewardChoice> CreateRareRewardChoices(int riskLevel, double multiplier)
    {
        var baseHeal = riskLevel / 4 + 15;
        var healAmount = _rewardPowerScaler.ScaleAmount(baseHeal, multiplier);

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
                $"Récupère {_rewardPowerScaler.ScaleAmount(baseHeal + 5, multiplier)} PV.",
                $"heal:{_rewardPowerScaler.ScaleAmount(baseHeal + 5, multiplier)}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Soin substantiel",
                $"Récupère {_rewardPowerScaler.ScaleAmount(baseHeal + 10, multiplier)} PV.",
                $"heal:{_rewardPowerScaler.ScaleAmount(baseHeal + 10, multiplier)}")
        };
    }

    private List<RewardChoice> CreateEliteRewardChoices(int riskLevel, double multiplier)
    {
        var baseHeal = riskLevel / 3 + 20;
        var healAmount = _rewardPowerScaler.ScaleAmount(baseHeal, multiplier);

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
                $"Récupère {_rewardPowerScaler.ScaleAmount(baseHeal + 8, multiplier)} PV.",
                $"heal:{_rewardPowerScaler.ScaleAmount(baseHeal + 8, multiplier)}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Suture mentale",
                $"Récupère {_rewardPowerScaler.ScaleAmount(baseHeal + 16, multiplier)} PV.",
                $"heal:{_rewardPowerScaler.ScaleAmount(baseHeal + 16, multiplier)}")
        };
    }

    private List<RewardChoice> CreateBossRewardChoices(int riskLevel, double multiplier)
    {
        var baseHeal = riskLevel / 2 + 30;
        var healAmount = _rewardPowerScaler.ScaleAmount(baseHeal, multiplier);

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
                $"Récupère {_rewardPowerScaler.ScaleAmount(baseHeal + 12, multiplier)} PV.",
                $"heal:{_rewardPowerScaler.ScaleAmount(baseHeal + 12, multiplier)}"),

            RewardChoice.Create(
                RewardType.Heal,
                "Silence recomposé",
                $"Récupère {_rewardPowerScaler.ScaleAmount(baseHeal + 24, multiplier)} PV.",
                $"heal:{_rewardPowerScaler.ScaleAmount(baseHeal + 24, multiplier)}")
        };
    }
}
