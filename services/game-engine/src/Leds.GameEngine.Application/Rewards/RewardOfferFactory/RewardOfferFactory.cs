using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.Application.Rewards.RewardOfferFactory;

public sealed class RewardOfferFactory
{
    public RewardOffer CreateCombatRewardOffer(RewardSource source, int riskLevel)
    {
        var healAmount = riskLevel / 5 + 10;

        var choices = new List<RewardChoice>
        {
            RewardChoice.Create(
                RewardType.Heal,
                $"Soin léger",
                $"Récupère {healAmount} PV.",
                $"heal:{healAmount}"),

            RewardChoice.Create(
                RewardType.StatBonus,
                "Bonus d'attaque",
                "Augmente l'attaque de 3 pour la run.",
                "stat_bonus:attack:3"),

            RewardChoice.Create(
                RewardType.MemoryFragment,
                "Fragment de Mémoire",
                "Un fragment de souvenir du Palais.",
                "memory_fragment:common")
        };

        return RewardOffer.Create(source, choices);
    }
}
