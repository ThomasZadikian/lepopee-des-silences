namespace Leds.GameEngine.Domain.Rewards;

/// <summary>One entry in an enemy's full possible loot table (not just what rolled).</summary>
public sealed record DefeatedEnemyLootEntry(
    string ItemKey,
    string ItemDisplayName,
    string Rarity,
    int DropPercent);

/// <summary>
/// Flavor + full loot-table summary for one defeated enemy (or a group of
/// identical ones), surfaced on the reward screen alongside the rolled
/// <see cref="RewardChoice"/> results.
/// </summary>
public sealed record DefeatedEnemySummary(
    string EnemyKey,
    string DisplayName,
    string Description,
    int Count,
    IReadOnlyCollection<DefeatedEnemyLootEntry> LootEntries);
