using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Rewards.RewardOfferFactory;

public sealed class RewardOfferFactory
{
    private readonly ICombatRiskProfileResolver _riskProfileResolver;
    private readonly ICatalogContentGateway _catalogContentGateway;
    private readonly EnemyLootRewardBuilder _enemyLootRewardBuilder;
    private readonly RewardPowerScaler _rewardPowerScaler = new();

    public RewardOfferFactory(
        ICombatRiskProfileResolver riskProfileResolver,
        ICatalogContentGateway catalogContentGateway,
        EnemyLootRewardBuilder enemyLootRewardBuilder)
    {
        _riskProfileResolver = riskProfileResolver;
        _catalogContentGateway = catalogContentGateway;
        _enemyLootRewardBuilder = enemyLootRewardBuilder;
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
            RewardSource.Elite => CreateEliteRewardChoices(riskLevel, scaling.RewardPowerMultiplier),
            RewardSource.Rare => CreateRareRewardChoices(riskLevel, scaling.RewardPowerMultiplier),
            _ => CreateCombatRewardChoices(riskLevel, scaling.RewardPowerMultiplier)
        };

        return RewardOffer.Create(source, choices, scaling);
    }

    /// <summary>
    /// Combat-flavoured reward offer whose candidate choices are rolled from the loot
    /// tables of the enemies actually present in the fight (see <see cref="EnemyLootRewardBuilder"/>),
    /// instead of the hardcoded per-tier heal choices <see cref="CreateCombatRewardOffer"/> uses.
    /// The player still picks exactly one choice from the offer, same as any other reward —
    /// only how the candidate pool is generated changes. Falls back to the hardcoded tier
    /// choices if the enemies present yielded no loot at all (e.g. catalog unreachable, or
    /// none of them have a loot table configured yet).
    /// </summary>
    public async Task<RewardOffer> CreateCombatRewardOfferAsync(
        RewardSource source,
        NodeEventType eventType,
        int riskLevel,
        IReadOnlyCollection<Combatant> enemies,
        string runSeed,
        Guid runId,
        Guid combatId,
        CancellationToken cancellationToken = default)
    {
        var scaling = _riskProfileResolver.Resolve(eventType, riskLevel);

        var choices = await _enemyLootRewardBuilder.BuildAsync(runSeed, runId, combatId, enemies, cancellationToken);

        if (choices.Count == 0)
        {
            choices = source switch
            {
                RewardSource.RoomBoss => CreateBossRewardChoices(riskLevel, scaling.RewardPowerMultiplier),
                RewardSource.Elite => CreateEliteRewardChoices(riskLevel, scaling.RewardPowerMultiplier),
                RewardSource.Rare => CreateRareRewardChoices(riskLevel, scaling.RewardPowerMultiplier),
                _ => CreateCombatRewardChoices(riskLevel, scaling.RewardPowerMultiplier)
            };
        }

        var defeatedEnemies = await _enemyLootRewardBuilder.BuildDefeatedEnemySummariesAsync(enemies, cancellationToken);

        return RewardOffer.Create(source, choices, scaling, defeatedEnemies);
    }

    public async Task<RewardOffer?> CreateFromTemplateKeyAsync(
        string templateKey,
        int riskLevel,
        CancellationToken cancellationToken = default)
    {
        var templateResult = await _catalogContentGateway.GetRewardTemplateByKeyAsync(templateKey, cancellationToken);
        if (templateResult.IsFailure) return null;

        var template = templateResult.Value;

        var choices = template.Options.Select(opt =>
        {
            var rewardType = Enum.Parse<RewardType>(opt.RewardType);
            var payloadKey = opt.PayloadType switch
            {
                "Item" when opt.PayloadKey is not null => $"item:{opt.PayloadKey}:{opt.Label}:{opt.Description}:Consumable:Common:Heal:{opt.BaseAmount}",
                _ when opt.PayloadKey is not null => opt.PayloadKey,
                _ => $"heal:{opt.BaseAmount}"
            };
            return RewardChoice.Create(
                rewardType,
                opt.Label,
                opt.Description,
                payloadKey);
        }).ToList();

        return RewardOffer.Create(
            Enum.TryParse<RewardSource>(template.SourceType, ignoreCase: true, out var source) ? source : RewardSource.Combat,
            choices);
    }

    public RewardOffer CreateMerchantRewardOffer(int riskLevel)
    {
        var choices = CreateMerchantRewardChoices(riskLevel);
        return RewardOffer.Create(RewardSource.NodeEvent, choices);
    }

    public RewardOffer CreateItemRewardOffer(
        string rewardProfile,
        int riskLevel,
        IReadOnlyCollection<RunModifier>? runModifiers = null)
    {
        // "Loi de l'Abondance" (law.abondance): item nodes propose a 4th choice.
        var extraChoiceEnabled = runModifiers?
            .Any(m => m.Type == RunModifierType.AbondanceExtraChoiceEnabled && !m.IsConsumed) ?? false;

        var choices = CreateItemRewardChoices(rewardProfile, riskLevel, extraChoiceEnabled);
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

    private List<RewardChoice> CreateItemRewardChoices(
        string rewardProfile, int riskLevel, bool extraChoiceEnabled = false)
    {
        // Memory rooms give a guard shard + a heal option — thematically aligned with
        // the exploration / knowledge flavour of the Memory biome.
        // All other Item node profiles default to a guard shard + heal combo.
        var baseHeal = riskLevel / 5 + 10;

        var choices = new List<RewardChoice>
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

        // "Loi de l'Abondance": a 4th choice. Documented simplification — the SFD's "un
        // nœud sur deux est vide à l'ouverture" half is not modeled, so this node always
        // gets the extra choice while the law is active, never a zero-choice node.
        if (extraChoiceEnabled)
        {
            choices.Add(RewardChoice.Create(
                RewardType.TemporaryItem,
                "Surplus du Palais",
                "Le Palais offre un quatrième choix — un nœud sur deux le regrettera.",
                "item:item.consumable.minor-heal:Surplus du Palais:Le Palais offre un quatrième choix — un nœud sur deux le regrettera.:Consumable:Common:Heal:15"));
        }

        return choices;
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
