using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.Application.Rewards.RewardOfferFactory;

public sealed class RewardOfferFactory
{
    public RewardOffer CreateCombatRewardOffer(RewardSource source, int riskLevel)
    {
        var choices = source switch
        {
            RewardSource.RoomBoss => CreateBossRewardChoices(riskLevel),
            RewardSource.Elite    => CreateEliteRewardChoices(riskLevel),
            RewardSource.Rare     => CreateRareRewardChoices(riskLevel),
            _                     => CreateCombatRewardChoices(riskLevel)
        };

        return RewardOffer.Create(source, choices);
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
    }

    private static List<RewardChoice> CreateRareRewardChoices(int riskLevel)
    {
        var healAmount = riskLevel / 4 + 15;

        return new List<RewardChoice>
        {
            RewardChoice.Create(
                RewardType.MemoryFragment,
                "Fragment Rare",
                "Un fragment exceptionnel du Palais.",
                "memory_fragment:rare"),

            RewardChoice.Create(
                RewardType.StatBonus,
                "Bonus de puissance",
                "Augmente l'attaque de 5 pour la run.",
                "stat_bonus:attack:5"),

            RewardChoice.Create(
                RewardType.Heal,
                "Soin substantiel",
                $"Récupère {healAmount} PV.",
                $"heal:{healAmount}")
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
                RewardType.StatBonus,
                "Bonus de défense",
                "Augmente la défense de 5 pour la run.",
                "stat_bonus:defense:5"),

            RewardChoice.Create(
                RewardType.MemoryFragment,
                "Fragment d'Élite",
                "Un fragment de haute valeur du Palais.",
                "memory_fragment:elite")
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
                RewardType.StatBonus,
                "Puissance du Palais",
                "Augmente l'attaque et la défense de 5 pour la run.",
                "stat_bonus:all:5"),

            RewardChoice.Create(
                RewardType.MemoryFragment,
                "Fragment du Gardien",
                "Un fragment de mémoire du boss du Palais.",
                "memory_fragment:boss")
        };
    }
}
