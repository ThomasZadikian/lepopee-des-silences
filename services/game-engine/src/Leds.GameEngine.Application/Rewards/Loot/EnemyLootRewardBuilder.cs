using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Selection;

namespace Leds.GameEngine.Application.Rewards.Loot;

/// <summary>
/// Rolls a combat's enemy-specific loot tables into a set of reward choices.
/// Each enemy present in the fight contributes 1-3 items rolled independently
/// against its own table's drop percentages; results are combined, capped at
/// <see cref="MaxLootCount"/>, and padded from the generic fallback pool up to
/// <see cref="MinLootCount"/> when the enemies present didn't roll enough on
/// their own. All randomness goes through <see cref="DeterministicSampler"/> so
/// a run stays replayable from its seed.
/// </summary>
public sealed class EnemyLootRewardBuilder
{
    public const int MinLootCount = 3;
    public const int MaxLootCount = 6;
    private const int MinPerEnemy = 1;
    private const int MaxPerEnemy = 3;

    private readonly ICatalogContentGateway _catalogContentGateway;

    public EnemyLootRewardBuilder(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    private sealed record RolledLoot(string ItemDefinitionKey, string? SourceEnemyKey, string? SourceEnemyDisplayName);

    public async Task<IReadOnlyCollection<RewardChoice>> BuildAsync(
        string runSeed,
        Guid runId,
        Guid combatId,
        IReadOnlyCollection<Combatant> enemies,
        CancellationToken cancellationToken = default)
    {
        var rolled = new List<RolledLoot>();
        var step = 0;

        foreach (var enemy in enemies)
        {
            var table = await _catalogContentGateway.GetEnemyLootTableByKeyAsync(enemy.SourceKey, cancellationToken);
            if (table is null || table.Entries.Count == 0)
            {
                continue; // No loot table configured yet for this enemy — expected during partial content rollout.
            }

            var hits = RollIndependent(table.Entries, runSeed, runId, combatId, ref step);
            hits = ClampCount(hits, table.Entries, MinPerEnemy, MaxPerEnemy, runSeed, runId, combatId, ref step);

            rolled.AddRange(hits.Select(itemKey => new RolledLoot(itemKey, enemy.SourceKey, enemy.DisplayName)));
        }

        if (rolled.Count > MaxLootCount)
        {
            rolled = TrimToUniformRandom(rolled, MaxLootCount, runSeed, runId, combatId, ref step);
        }

        if (rolled.Count < MinLootCount)
        {
            rolled = await PadFromFallbackAsync(rolled, runSeed, runId, combatId, step, cancellationToken);
        }

        var choices = new List<RewardChoice>();
        foreach (var loot in rolled)
        {
            var itemResult = await _catalogContentGateway.GetItemDefinitionByKeyAsync(loot.ItemDefinitionKey, cancellationToken);
            if (itemResult.IsFailure)
            {
                continue; // Item key referenced by a loot table but not (yet) defined in the catalog.
            }

            var item = itemResult.Value;
            choices.Add(RewardChoice.Create(
                RewardType.TemporaryItem,
                item.DisplayName,
                item.Description,
                $"item:{item.Key}:{item.DisplayName}:{item.Description}:{item.Category}:{item.Rarity}:{item.ItemType}:0",
                loot.SourceEnemyKey,
                loot.SourceEnemyDisplayName));
        }

        return choices;
    }

    private async Task<List<RolledLoot>> PadFromFallbackAsync(
        List<RolledLoot> rolled,
        string runSeed,
        Guid runId,
        Guid combatId,
        int step,
        CancellationToken cancellationToken)
    {
        var fallback = await _catalogContentGateway.GetActiveGenericLootPoolAsync(cancellationToken);
        if (fallback is null || fallback.Entries.Count == 0)
        {
            return rolled;
        }

        var needed = MinLootCount - rolled.Count;

        var hits = RollIndependent(fallback.Entries, runSeed, runId, combatId, ref step);
        // Target the fallback hit count at exactly what's needed: pad from the highest-%
        // entries if the independent rolls came up short, trim uniformly if they overshot.
        hits = ClampCount(hits, fallback.Entries, needed, needed, runSeed, runId, combatId, ref step);

        rolled.AddRange(hits.Select(itemKey => new RolledLoot(itemKey, SourceEnemyKey: null, SourceEnemyDisplayName: null)));

        if (rolled.Count > MaxLootCount)
        {
            rolled = TrimToUniformRandom(rolled, MaxLootCount, runSeed, runId, combatId, ref step);
        }

        return rolled;
    }

    private static List<string> RollIndependent(
        IReadOnlyCollection<CatalogLootEntry> entries,
        string runSeed,
        Guid runId,
        Guid combatId,
        ref int step)
    {
        var hits = new List<string>();
        foreach (var entry in entries)
        {
            var sample = DeterministicSampler.NextUnitInterval(runSeed, runId, combatId, step++);
            if (sample < entry.DropPercent / 100m)
            {
                hits.Add(entry.ItemDefinitionKey);
            }
        }

        return hits;
    }

    /// <summary>
    /// Forces the hit count into [min, max]: pads with the highest-drop-% entries not
    /// already hit (deterministic, no extra sampling needed), and trims excess hits via
    /// a deterministic uniform shuffle (not weighted/prioritized, per spec).
    /// </summary>
    private static List<string> ClampCount(
        List<string> hits,
        IReadOnlyCollection<CatalogLootEntry> entries,
        int min,
        int max,
        string runSeed,
        Guid runId,
        Guid combatId,
        ref int step)
    {
        if (hits.Count < min)
        {
            var candidates = entries
                .Where(e => !hits.Contains(e.ItemDefinitionKey))
                .OrderByDescending(e => e.DropPercent);

            foreach (var candidate in candidates)
            {
                if (hits.Count >= min) break;
                hits.Add(candidate.ItemDefinitionKey);
            }
        }

        if (hits.Count > max)
        {
            hits = TrimToUniformRandom(hits, max, runSeed, runId, combatId, ref step);
        }

        return hits;
    }

    private static List<string> TrimToUniformRandom(
        List<string> items,
        int max,
        string runSeed,
        Guid runId,
        Guid combatId,
        ref int step)
    {
        var keyed = new List<(string Item, decimal SortKey)>(items.Count);
        foreach (var item in items)
        {
            keyed.Add((item, DeterministicSampler.NextUnitInterval(runSeed, runId, combatId, step++)));
        }

        return keyed
            .OrderBy(x => x.SortKey)
            .Take(max)
            .Select(x => x.Item)
            .ToList();
    }

    private static List<RolledLoot> TrimToUniformRandom(
        List<RolledLoot> items,
        int max,
        string runSeed,
        Guid runId,
        Guid combatId,
        ref int step)
    {
        var keyed = new List<(RolledLoot Item, decimal SortKey)>(items.Count);
        foreach (var item in items)
        {
            keyed.Add((item, DeterministicSampler.NextUnitInterval(runSeed, runId, combatId, step++)));
        }

        return keyed
            .OrderBy(x => x.SortKey)
            .Take(max)
            .Select(x => x.Item)
            .ToList();
    }
}
