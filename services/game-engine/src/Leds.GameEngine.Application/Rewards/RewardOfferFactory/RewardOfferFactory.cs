using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Domain.Selection;

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
    /// Creates a combat reward offer for the given source tier and combat risk tier.
    /// The <paramref name="eventType"/> is forwarded to <see cref="ICombatRiskProfileResolver"/>
    /// so that the resulting <see cref="RewardOffer.CombatScaling"/> reflects the tier's
    /// computed multipliers. <paramref name="riskLevel"/> is the node's combat risk tier
    /// (1-5, see <see cref="Leds.GameEngine.Domain.Combats.RiskTier"/>) — NOT the raw
    /// 0-100 <c>MapNode.RiskLevel</c> used by Item/Merchant reward generation.
    /// </summary>
    public RewardOffer CreateCombatRewardOffer(
        RewardSource source,
        NodeEventType eventType,
        int riskLevel)
    {
        var scaling = _riskProfileResolver.Resolve(eventType, riskLevel);

        var choices = source switch
        {
            RewardSource.RoomBoss => CreateBossRewardChoices(riskLevel, scaling.DifficultyMultiplier),
            RewardSource.Elite => CreateEliteRewardChoices(riskLevel, scaling.DifficultyMultiplier),
            RewardSource.Rare => CreateRareRewardChoices(riskLevel, scaling.DifficultyMultiplier),
            _ => CreateCombatRewardChoices(riskLevel, scaling.DifficultyMultiplier)
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
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<RunModifier>? runModifiers = null)
    {
        var scaling = _riskProfileResolver.Resolve(eventType, riskLevel);

        // "Loi de l'Invitation" (law.invitation): combat loot item drop chances are
        // boosted. The SFD's matching "+10% Éclats" half is a documented gap — see
        // RunModifierType.LootChanceBonusPercent.
        var lootChanceBonusPercent = runModifiers?
            .Where(m => m.Type == RunModifierType.LootChanceBonusPercent && !m.IsConsumed)
            .Sum(m => m.Value) ?? 0;

        var choices = await _enemyLootRewardBuilder.BuildAsync(
            runSeed, runId, combatId, enemies, lootChanceBonusPercent, cancellationToken, scaling.LootMultiplier);

        if (choices.Count == 0)
        {
            choices = source switch
            {
                RewardSource.RoomBoss => CreateBossRewardChoices(riskLevel, scaling.DifficultyMultiplier),
                RewardSource.Elite => CreateEliteRewardChoices(riskLevel, scaling.DifficultyMultiplier),
                RewardSource.Rare => CreateRareRewardChoices(riskLevel, scaling.DifficultyMultiplier),
                _ => CreateCombatRewardChoices(riskLevel, scaling.DifficultyMultiplier)
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

        var choices = template.Options.Select(BuildRewardChoiceFromOption).ToList();

        return RewardOffer.Create(
            Enum.TryParse<RewardSource>(template.SourceType, ignoreCase: true, out var source) ? source : RewardSource.Combat,
            choices);
    }

    private static RewardChoice BuildRewardChoiceFromOption(CatalogRewardTemplateOptionSnapshot option)
    {
        var rewardType = Enum.Parse<RewardType>(option.RewardType);
        return RewardChoice.Create(rewardType, option.Label, option.Description, BuildPayloadKey(option));
    }

    private static string BuildPayloadKey(CatalogRewardTemplateOptionSnapshot option)
    {
        if (option.PayloadType == "Item" && option.PayloadKey is not null)
        {
            // ItemType/ItemRarity/ItemEffectType default to Consumable/Common/Heal for
            // templates authored before these fields existed — every "reward.item.*"
            // template seeded going forward carries its own real values (see
            // CatalogSeedRunner).
            var itemType = option.ItemType ?? "Consumable";
            var itemRarity = option.ItemRarity ?? "Common";
            var itemEffectType = option.ItemEffectType ?? "Heal";
            return $"item:{option.PayloadKey}:{option.Label}:{option.Description}:{itemType}:{itemRarity}:{itemEffectType}:{option.BaseAmount}";
        }

        return option.PayloadKey ?? $"heal:{option.BaseAmount}";
    }

    public RewardOffer CreateMerchantRewardOffer(int riskLevel)
    {
        var choices = CreateMerchantRewardChoices(riskLevel);
        return RewardOffer.Create(RewardSource.NodeEvent, choices);
    }

    /// <summary>Catalog key for the item-node reward pool. "Loi de la Chandelle"
    /// (law.chandelle) rerolls draw a different subset of this same pool — see
    /// SampleOptionsDeterministically.</summary>
    private const string ItemRewardTemplateKey = "reward.item.default";

    /// <summary>
    /// Creates an item-node reward offer by deterministically sampling from the
    /// catalog-authored "reward.item.default" template's option pool (see
    /// CatalogSeedRunner) — the pool may hold more options than are shown at once, so
    /// "Loi de la Chandelle" (law.chandelle) can reroll into a genuinely different
    /// subset by passing a different <paramref name="rerollNonce"/>. Falls back to the
    /// small hardcoded baseline (<see cref="CreateItemRewardChoices"/>) if the catalog
    /// template is unreachable or missing, so an item node is never left with zero
    /// choices.
    /// </summary>
    public async Task<RewardOffer> CreateItemRewardOfferAsync(
        string rewardProfile,
        int riskLevel,
        IReadOnlyCollection<RunModifier>? runModifiers,
        string runSeed,
        Guid runId,
        Guid nodeId,
        int rerollNonce = 0,
        CancellationToken cancellationToken = default)
    {
        // "Loi de l'Abondance" (law.abondance): item nodes propose a 4th choice.
        var extraChoiceEnabled = runModifiers?
            .Any(m => m.Type == RunModifierType.AbondanceExtraChoiceEnabled && !m.IsConsumed) ?? false;

        var templateResult = await _catalogContentGateway.GetRewardTemplateByKeyAsync(
            ItemRewardTemplateKey, cancellationToken);

        if (templateResult is null || templateResult.IsFailure || templateResult.Value.Options.Count == 0)
        {
            var fallbackChoices = CreateItemRewardChoices(rewardProfile, riskLevel, extraChoiceEnabled);
            return RewardOffer.Create(RewardSource.NodeEvent, fallbackChoices);
        }

        var template = templateResult.Value;
        var desiredCount = extraChoiceEnabled ? template.MaxChoices + 1 : template.MaxChoices;
        var sampled = SampleOptionsDeterministically(
            template.Options, desiredCount, runSeed, runId, nodeId, rerollNonce);

        var choices = sampled.Select(BuildRewardChoiceFromOption).ToList();
        return RewardOffer.Create(RewardSource.NodeEvent, choices);
    }

    /// <summary>
    /// Picks <paramref name="count"/> distinct options from <paramref name="pool"/>,
    /// deterministic from (seed, runId, contextId, rerollNonce) — same inputs always
    /// produce the same subset, a different rerollNonce (almost always) produces a
    /// different one. Clamped to the pool's size, since a reroll charge should never
    /// throw just because the pool hasn't been authored with enough variety yet.
    /// </summary>
    private static List<CatalogRewardTemplateOptionSnapshot> SampleOptionsDeterministically(
        IReadOnlyCollection<CatalogRewardTemplateOptionSnapshot> pool,
        int count,
        string seed,
        Guid runId,
        Guid contextId,
        int rerollNonce)
    {
        var poolList = pool.ToList();
        var take = Math.Clamp(count, 0, poolList.Count);

        return poolList
            .Select((option, index) => (option, sortKey: DeterministicSampler.NextUnitInterval(
                seed, runId, contextId, (index * 1000) + rerollNonce)))
            .OrderBy(entry => entry.sortKey)
            .Take(take)
            .Select(entry => entry.option)
            .ToList();
    }

    private List<RewardChoice> CreateCombatRewardChoices(int riskLevel, double multiplier)
    {
        // BALANCE KNOB — riskLevel is now the 1-5 combat risk tier (was 0-100); rescaled to
        // roughly preserve the old 10-30 heal range across the 5 discrete tiers.
        var baseHeal = (riskLevel - 1) * 5 + 10;
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
        // BALANCE KNOB — rescaled from a 0-100 input to the 1-5 tier (old range ~15-40).
        var baseHeal = (riskLevel - 1) * 6 + 15;
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
        // BALANCE KNOB — rescaled from a 0-100 input to the 1-5 tier (old range ~20-53).
        var baseHeal = (riskLevel - 1) * 8 + 20;
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
        // BALANCE KNOB — rescaled from a 0-100 input to the 1-5 tier (old range ~30-80).
        var baseHeal = (riskLevel - 1) * 12 + 30;
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
