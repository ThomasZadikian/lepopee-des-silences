using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Domain.Selection;

namespace Leds.GameEngine.Application.Rewards.Loot;

/// <summary>
/// A small, secondary drop of combat loot that lands physically on the ground instead of in
/// the curated reward-choice offer (see RewardOfferFactory/EnemyLootRewardBuilder, which this
/// never touches) — the "Diablo ambiance" layer: a glowing pickup or two where the fight
/// happened, on top of the deliberate reward choice, not instead of it.
///
/// Draws from the same generic/minor loot pool EnemyLootRewardBuilder already uses to pad its
/// own offer (ICatalogContentGateway.GetActiveGenericLootPoolAsync) — no new catalog concept.
/// Uses a namespaced step offset into DeterministicSampler so this roll sequence never collides
/// with EnemyLootRewardBuilder's own step counter for the same (runSeed, runId, combatId)
/// triple — both are keyed by that same combat, and without the offset they'd draw from
/// literally the same hash sequence.
/// </summary>
public sealed class GroundLootBuilder
{
    // BALANCE KNOB — how many ground pickups a single combat can drop, independent of (and
    // additive to) the curated reward-choice offer's own item count.
    private const int MinDropCount = 1;
    private const int MaxDropCount = 2;

    private const int StepOffset = 10_000;

    private readonly ICatalogContentGateway _catalogContentGateway;

    public GroundLootBuilder(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    public async Task<IReadOnlyCollection<RunItem>> BuildAsync(
        string runSeed,
        Guid runId,
        Guid combatId,
        CancellationToken cancellationToken = default)
    {
        var pool = await _catalogContentGateway.GetActiveGenericLootPoolAsync(cancellationToken);
        if (pool is null || pool.Entries.Count == 0)
        {
            return [];
        }

        var step = StepOffset;
        var hits = new List<string>();

        foreach (var entry in pool.Entries)
        {
            var sample = DeterministicSampler.NextUnitInterval(runSeed, runId, combatId, step++);
            if (sample < entry.DropPercent / 100m)
            {
                hits.Add(entry.ItemDefinitionKey);
            }

            if (hits.Count >= MaxDropCount)
            {
                break;
            }
        }

        if (hits.Count < MinDropCount)
        {
            // Deterministic pad from the highest-drop-% entries not already hit — same shape as
            // EnemyLootRewardBuilder.ClampCount, no extra sampling needed.
            foreach (var entry in pool.Entries.OrderByDescending(e => e.DropPercent))
            {
                if (hits.Count >= MinDropCount) break;
                if (!hits.Contains(entry.ItemDefinitionKey))
                {
                    hits.Add(entry.ItemDefinitionKey);
                }
            }
        }

        var items = new List<RunItem>();
        foreach (var key in hits)
        {
            var itemResult = await _catalogContentGateway.GetItemDefinitionByKeyAsync(key, cancellationToken);
            if (itemResult.IsFailure)
            {
                continue; // Item key referenced by the pool but not (yet) defined in the catalog.
            }

            var item = itemResult.Value;
            // Ground drops are RunItems by construction — a permanent-eligible item (modèle
            // Hadès: anything meant to be equipped) must never enter the run's temporary
            // inventory at all, ground or otherwise. The generic pool is a minor/common
            // consumable pool in practice; this is a defensive guard against a future
            // authoring mistake, not an expected case.
            if (item.IsPermanentEligible)
            {
                continue;
            }

            items.Add(RunItem.Create(
                item.Key,
                item.DisplayName,
                item.Description,
                CatalogRunItemMapper.MapType(item.Category),
                CatalogRunItemMapper.MapRarity(item.Rarity),
                quantity: 1,
                CatalogRunItemMapper.MapEffect(item.EffectRunType),
                effectAmount: item.EffectValue));
        }

        return items;
    }
}
