using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
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
            var runItemRarity = MapToRunItemRarity(item.Rarity);
            var runItemEffectType = item.IsUsableInCombat ? "Heal" : "None";
            choices.Add(RewardChoice.Create(
                RewardType.TemporaryItem,
                item.DisplayName,
                item.Description,
                $"item:{item.Key}:{item.DisplayName}:{item.Description}:Consumable:{runItemRarity}:{runItemEffectType}:0",
                loot.SourceEnemyKey,
                loot.SourceEnemyDisplayName));
        }

        return choices;
    }

    // Catalog rarities (Common/Uncommon/Rare/Epic/Legendary/Unique) are a superset of
    // RunItemRarity (Common/Uncommon/Rare/Epic only) — anything above Epic collapses to
    // Epic rather than throwing when a run item is materialized from catalog loot.
    private static string MapToRunItemRarity(string catalogRarity) => catalogRarity switch
    {
        "Common" or "Uncommon" or "Rare" or "Epic" => catalogRarity,
        _ => "Epic"
    };

    /// <summary>
    /// Builds a display-only summary of every enemy defeated in the fight — description
    /// plus the *full* loot table each could have dropped, not just what actually rolled
    /// in <see cref="BuildAsync"/>. Identical enemies (same catalog key) are grouped into
    /// one summary with a count, rather than one card per combatant instance.
    /// </summary>
    public async Task<IReadOnlyCollection<DefeatedEnemySummary>> BuildDefeatedEnemySummariesAsync(
        IReadOnlyCollection<Combatant> enemies,
        CancellationToken cancellationToken = default)
    {
        var summaries = new List<DefeatedEnemySummary>();
        var itemCache = new Dictionary<string, CatalogItemDefinitionSnapshot?>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in enemies.Where(e => e.IsDefeated).GroupBy(e => e.SourceKey, StringComparer.OrdinalIgnoreCase))
        {
            var enemyKey = group.Key;
            var displayName = group.First().DisplayName;

            var enemyDefinition = await _catalogContentGateway.GetEnemyDefinitionByKeyAsync(enemyKey, cancellationToken);
            var lootTable = await _catalogContentGateway.GetEnemyLootTableByKeyAsync(enemyKey, cancellationToken);

            var lootEntries = new List<DefeatedEnemyLootEntry>();
            foreach (var entry in lootTable?.Entries ?? [])
            {
                if (!itemCache.TryGetValue(entry.ItemDefinitionKey, out var item))
                {
                    var itemResult = await _catalogContentGateway.GetItemDefinitionByKeyAsync(entry.ItemDefinitionKey, cancellationToken);
                    item = itemResult.IsSuccess ? itemResult.Value : null;
                    itemCache[entry.ItemDefinitionKey] = item;
                }

                if (item is null)
                {
                    continue; // Item key referenced by a loot table but not (yet) defined in the catalog.
                }

                lootEntries.Add(new DefeatedEnemyLootEntry(item.Key, item.DisplayName, item.Rarity, entry.DropPercent));
            }

            summaries.Add(new DefeatedEnemySummary(
                enemyKey,
                displayName,
                enemyDefinition?.Description ?? string.Empty,
                group.Count(),
                lootEntries));
        }

        return summaries;
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
