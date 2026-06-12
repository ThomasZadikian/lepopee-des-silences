using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;

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

    public RewardOffer CreateMerchantRewardOffer(int riskLevel)
    {
        var choices = CreateMerchantRewardChoices(riskLevel);
        return RewardOffer.Create(RewardSource.NodeEvent, choices);
    }

    public RewardOffer CreateItemRewardOffer(string rewardProfile, int riskLevel)
    {
        var choices = CreateItemRewardChoices(rewardProfile, riskLevel);
        return RewardOffer.Create(RewardSource.NodeEvent, choices);
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
                RewardType.TemporaryItem,
                "Baume de mémoire",
                "Restaure 15 PV depuis l'inventaire.",
                "item:item.consumable.minor-heal:Baume de mémoire:Restaure une partie de la vitalité.:Consumable:Common:Heal:15")
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

    private List<RewardChoice> CreateItemRewardChoices(string rewardProfile, int riskLevel)
    {
        // Memory rooms give a guard shard + a heal option — thematically aligned with
        // the exploration / knowledge flavour of the Memory biome.
        // All other Item node profiles default to a guard shard + heal combo.
        var baseHeal = riskLevel / 5 + 10;

        return new List<RewardChoice>
        {
            RewardChoice.Create(
                RewardType.TemporaryItem,
                "Éclat de garde",
                "Offre une protection permanente pendant la run.",
                "item:item.consumable.guard-shard:Éclat de garde:Offre une protection permanente pendant la run.:Consumable:Uncommon:Guard:8"),

            RewardChoice.Create(
                RewardType.TemporaryItem,
                "Baume de mémoire",
                "Restaure une partie de la vitalité.",
                "item:item.consumable.minor-heal:Baume de mémoire:Restaure une partie de la vitalité.:Consumable:Common:Heal:15"),

            RewardChoice.Create(
                RewardType.Heal,
                "Souffle du passé",
                $"Récupère {baseHeal} PV.",
                $"heal:{baseHeal}")
        };
    }

    private List<RewardChoice> CreateMerchantRewardChoices(int riskLevel)
    {
        var baseHeal = riskLevel / 5 + 12;

        return new List<RewardChoice>
        {
            RewardChoice.Create(
                RewardType.TemporaryItem,
                "Baume de mémoire",
                "Restaure une partie de la vitalité.",
                "item:item.consumable.minor-heal:Baume de mémoire:Restaure une partie de la vitalité.:Consumable:Common:Heal:15"),

            RewardChoice.Create(
                RewardType.TemporaryItem,
                "Éclat de garde",
                "Offre une protection temporaire.",
                "item:item.consumable.guard-shard:Éclat de garde:Offre une protection temporaire.:Consumable:Uncommon:Guard:8"),

            RewardChoice.Create(
                RewardType.Heal,
                "Soin du marchand",
                $"Récupère {baseHeal} PV.",
                $"heal:{baseHeal}")
        };
    }
}
